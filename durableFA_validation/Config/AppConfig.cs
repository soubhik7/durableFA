using System;
namespace durableFA_validation.Config
{
    public class AppConfig
    {
        public string NewtonsoftLicenseKey { get; set; }
        public string StorageConnectionString { get; set; }
        public string RawDataContainer { get; set; }
        public string ValidDataContainer { get; set; }
        public string InValidDataContainer { get; set; }
        public string SummaryTable { get; set; }
        public string AccountName { get; set; }
        public AppConfig()
        {
            this.NewtonsoftLicenseKey =
         Environment.GetEnvironmentVariable(
             $"{nameof(AppConfig)}:NewtonsoftLicenseKey");
            this.StorageConnectionString =
         Environment.GetEnvironmentVariable(
             $"{nameof(AppConfig)}:StorageConnectionString");


            this.RawDataContainer =
         Environment.GetEnvironmentVariable(
             $"{nameof(AppConfig)}:RawDataContainer");
            this.ValidDataContainer =
         Environment.GetEnvironmentVariable(
             $"{nameof(AppConfig)}:ValidDataContainer");
            this.InValidDataContainer =
         Environment.GetEnvironmentVariable(
             $"{nameof(AppConfig)}:InValidDataContainer");
            this.SummaryTable =
         Environment.GetEnvironmentVariable(
             $"{nameof(AppConfig)}:SummaryTable");
            this.AccountName =
         Environment.GetEnvironmentVariable(
             $"{nameof(AppConfig)}:AccountName");
        }
    }
}

