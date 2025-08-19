using Business.Abstract;
using Entities.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    public class AuthController : Controller
    {
        private IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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

            // Kullanıcı login olduysa access token oluştur
            var result = _authService.CreateAccessToken(userToLogin.Data);

            // Eğer access token oluşturma başarılıysa kullanıcıyı yönlendir
            if (result.Success)
            {
                // Kullanıcı token'ını session'a kaydet
                HttpContext.Session.SetString("UserToken", System.Text.Json.JsonSerializer.Serialize(result.Data));

                // Kullanıcıyı anasayfaya yönlendir
                return RedirectToAction("Index", "Home");
            }

            // Token oluşturma başarısızsa hata mesajı döndür
            ViewBag.ErrorMessage = result.Message;
            return View(userForLoginDto);
        }

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
                HttpContext.Session.SetString("UserToken",System.Text.Json.JsonSerializer.Serialize(result.Data));

                // Kullanıcıyı anasayfaya yönlendir
                return RedirectToAction("Index", "Home");
            }

            // Token oluşturma başarısızsa hata mesajı döndür
            ViewBag.ErrorMessage = result.Message;
            return View(userForRegisterDto);
        }
    }
}
