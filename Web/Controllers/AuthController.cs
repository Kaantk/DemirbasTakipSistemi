using Business.Abstract;
using Core.Utilities.Security.Hashing;
using Entities.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Controller]
    public class AuthController : Controller
    {
        private IAuthService _authService;
        private IUserService _userService;

        public AuthController(IAuthService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet("resetPassword")]
        public IActionResult ResetPassword(int id)
        {
            // Kullanıcı ID'sine göre kullanıcıyı al
            var user = _userService.GetById(id);

            // Eğer kullanıcı bulunamazsa login sayfasına yönlendir
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Eğer kullanıcı ForgotPassword durumunda değilse login sayfasına yönlendir 
            var model = new UserForResetPasswordDto { UserId = id };

            return View(model);
        }

        // Kullanıcı login sayfası işlemi
        [HttpPost("login")]
        public IActionResult Login(UserForLoginDto userForLoginDto)
        {
            // Kullanıcıyı login et
            var userToLogin = _authService.Login(userForLoginDto);

            // Eğer kullanıcı login olamadıysa hata mesajı döndür
            if (!userToLogin.Success)
            {
                ViewBag.ErrorMessage = userToLogin.Message;
                return View(userForLoginDto);
            }

            // Eğer kullanıcı ForgotPassword durumunda ise gerekli yönlendirme işlemlerini yap
            if (userToLogin.Data.ForgotPassword == true)
            {
                return RedirectToAction("resetPassword", "auth", new { id = userToLogin.Data.Id });
            }

            // Kullanıcı login olduysa access token oluştur
            var result = _authService.CreateAccessToken(userToLogin.Data);

            // Eğer access token oluşturma başarılıysa kullanıcıyı yönlendir
            if (result.Success)
            {
                // Kullanıcı token'ını session'a kaydet
                HttpContext.Session.SetString("UserToken", System.Text.Json.JsonSerializer.Serialize(result.Data));

                // Cookie'ye kaydet (string olarak)
                Response.Cookies.Append("UserToken",
                    System.Text.Json.JsonSerializer.Serialize(result.Data),
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddHours(1)
                    });

                // Kullanıcıyı anasayfaya yönlendir
                return RedirectToAction("index", "home");
            }

            // Token oluşturma başarısızsa hata mesajı döndür
            ViewBag.ErrorMessage = result.Message;
            return View(userForLoginDto);
        }

        // Kullanıcı kaydı için gerekli olan endpoint
        [HttpPost("register")]
        public IActionResult Register(UserForRegisterDto userForRegisterDto)
        {
            // Kullanıcı mail kontrolu
            var userExists = _authService.UserExists(userForRegisterDto.Email);

            // Kullanıcı zaten varsa hata mesajı döndür
            if (!userExists.Success)
            {
                ViewBag.ErrorMessage = userExists.Message;
                return View(userForRegisterDto);
            }

            // Kullanıcıyı kaydet ve access token oluştur
            var registerResult = _authService.Register(userForRegisterDto, userForRegisterDto.Password);
            var result = _authService.CreateAccessToken(registerResult.Data);

            // Access token oluşturma başarılıysa kullanıcıyı yönlendir
            if (result.Success)
            {
                HttpContext.Session.SetString("UserToken", System.Text.Json.JsonSerializer.Serialize(result.Data));

                // Kullanıcıyı anasayfaya yönlendir
                return RedirectToAction("index", "home");
            }

            // Token oluşturma başarısızsa hata mesajı döndür
            ViewBag.ErrorMessage = result.Message;
            return View(userForRegisterDto);
        }

        // Şifremi sıfırlama işlemi için gerekli olan endpoint
        [HttpPost("forgotPassword")]
        public IActionResult ForgotPassword(UserForForgotPasswordDto userForForgotPasswordDto)
        {
            // Kullanıcı mail kontrolü
            var user = _userService.GetByMail(userForForgotPasswordDto.Email);

            if (user != null)
            {
                // Kullanıcının ForgotPassword durumunu true yap
                user.ForgotPassword = true;
                _userService.Update(user);

                ViewBag.SuccessMessage = "Yeni şifreniz e-posta adresinize gönderildi. Lütfen kontrol ediniz.";
                return View();
            }
            else
            {
                // Kullanıcı yoksa hata mesajı göster
                ViewBag.ErrorMessage = "Bu e-posta adresi kayıtlı değil.";
                return View(userForForgotPasswordDto);
            }
        }

        // Yeni şifre oluşturma işlemi için gerekli olan endpoint
        [HttpPost("resetPassword")]
        public IActionResult ResetPassword(UserForResetPasswordDto userForResetPasswordDto)
        {
            // Şifreler eşleşmiyorsa hata mesajı döndür
            if (userForResetPasswordDto.NewPassword != userForResetPasswordDto.ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Şifreler eşleşmiyor.";
                return View(userForResetPasswordDto);
            }

            // Kullanıcıyı al
            var user = _userService.GetById(userForResetPasswordDto.UserId);

            // Eğer kullanıcı yoksa hata mesajı döndür
            if (user == null)
            {
                ViewBag.ErrorMessage = "Kullanıcı bulunamadı.";
                return View(userForResetPasswordDto);
            }

            // Hash işlemi
            byte[] passwordHash, passwordSalt;
            HashingHelper.CreatePasswordHash(userForResetPasswordDto.NewPassword, out passwordHash, out passwordSalt);

            // Kullanıcının şifresini güncelle
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            user.ForgotPassword = false; // Şifre sıfırlama işlemi tamamlandığında ForgotPassword durumunu false yap

            _userService.Update(user);

            ViewBag.SuccessMessage = "Şifre başarıyla güncellendi.";
            return View(userForResetPasswordDto);
        }
    }
}
