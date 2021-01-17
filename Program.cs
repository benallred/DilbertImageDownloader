using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace DilbertImageDownloader
{
   /// <summary>Downloads daily Dilbert comics.</summary>
   public class Program
   {
      private const string BaseUrl = "http://dilbert.com/strip/";
      private const string ImageUrlRegex = @"https://assets\.amuniversal\.com/\w+";
      private static readonly DateTime StartDate = new DateTime(1989, 4, 16);
      private const string SaveNameFormat = "Dilbert {date}.gif";

      // dotnet run --save-folder=C:\BenEx\Humor\Dilbert
      /// <param name="saveFolder">Base folder to save to. Subfolders will be created by year, as needed.</param>
      public static void Main(string saveFolder)
      {
         Console.WriteLine("Dilbert Image Downloader");

         if (string.IsNullOrWhiteSpace(saveFolder))
         {
            Console.WriteLine($"Must pass a value for {nameof(saveFolder)}.");
            return;
         }

         saveFolder = saveFolder.EndsWith('\\') ? saveFolder : saveFolder + "\\";

         while (true)
         {
            (DateTime date,
                string formattedDate,
                string foldername,
                string filename) = GetNextDayToSave(saveFolder);

            if (date > DateTime.Today)
            {
               break;
            }

            Directory.CreateDirectory(foldername);

            string page = GetImagePage(formattedDate);

            if (TryGetImageUrl(page, out string imageUrl))
            {
               Console.Write("Downloading " + new FileInfo(filename).Name + " ...");
               new WebClient().DownloadFile(imageUrl, filename);
               Console.WriteLine(" Done");
            }
            else
            {
               Console.WriteLine("Can't find image for " + new FileInfo(filename).Name);
               Console.WriteLine("Press Enter to continue.");
               Console.ReadLine();
               break;
            }
         }
      }

      private static
            (DateTime date,
            string formattedDate,
            string foldername,
            string filename) GetNextDayToSave(string saveFolder)
      {
         DateTime date = StartDate.AddDays(-1);
         string formattedDate;
         string foldername;
         string filename;

         do
         {
            date = date.AddDays(1);
            formattedDate = date.ToString("yyyy-MM-dd");
            foldername = Path.Combine(saveFolder, date.Year.ToString());
            filename = Path.Combine(foldername, SaveNameFormat.Replace("{date}", formattedDate));
         }
         while (File.Exists(filename));

         return (date, formattedDate, foldername, filename);
      }

      private static string GetImagePage(string formattedDate)
      {
         return new HttpClient().GetStringAsync(BaseUrl + formattedDate).Result;
      }

      private static bool TryGetImageUrl(string page, out string imageUrl)
      {
         Regex imageUrlRegex = new Regex(ImageUrlRegex);
         Match imageUrlMatch = imageUrlRegex.Match(page);
         imageUrl = imageUrlMatch.Value;
         return imageUrlMatch.Success;
      }
   }
}
