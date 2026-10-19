namespace UP01_Andreev_423_FoodDelivery
{
    public static class AppSession
    {
        
        public static int? CurrentUserId { get; set; }

        public static int FailedLoginAttempts { get; set; }
        public static string CaptchaCode { get; set; }

        public static bool ClearNavigationHistoryOnNextPage { get; set; }

        public static void ResetLoginSecurity()
        {
            FailedLoginAttempts = 0;
            CaptchaCode = null;
        }
    }
}
