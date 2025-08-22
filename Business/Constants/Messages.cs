using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities.Concrete;

namespace Business.Constants
{
    public static class Messages
    {
        public static string ProductAdded = "Ürün başarıyla eklendi.";
        public static string ProductUpdated = "Ürün başarıyla güncellendi.";
        public static string ProductDeleted = "Ürün başarıyla silindi.";

        public static string CategoryAdded = "Kategori başarıyla eklendi.";
        public static string CategoryUpdated = "Kategori başarıyla güncellendi.";
        public static string CategoryDeleted = "Kategori başarıyla silindi.";

        public static string UserNotFound = "Kullanıcı bulunamadı.";
        public static string PasswordError = "Lütfen şifrenizi kontrol ediniz.";
        public static string SuccessfulLogin = "Giriş başarılıdır.";

        public static string UserAlreadyExists = "Bu e-posta adresi zaten kayıtlı.";
        public static string UserRegistered = "Kullanıcı başarıyla kayıt edildi.";

        public static string AccessTokenCreated = "Access token başarıyla oluşturuldu";
    }
}
