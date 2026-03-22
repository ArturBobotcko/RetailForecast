namespace RetailForecast.Settings
{
    public class FileUploadSettings
    {
        public string RootPath { get; set; } = "./uploads";
        public bool CreateUserFolders { get; set; } = true;
        public string[] AllowedExtensions { get; set; } = { ".csv", ".xls", ".xlsx" };
    }
}
