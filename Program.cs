using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace DilbertImageDownloader
{
   public class Program
   {
      private const string BaseUrl = "http://dilbert.com/strip/";
      private const string ImageUrlRegex = @"data-image=""(//assets\.amuniversal\.com/[^""]*)""";
      private const string ImageBaseUrl = "https:";
      private static readonly DateTime StartDate = new DateTime(1989, 4, 16);
      private const string SaveNameFormat = "Dilbert {date}.gif";

      // dotnet run count=all autoclose=true savefolder=C:\BenEx\Humor\Dilbert readingfolder=C:\Ben\Desktop\Miguk\Dilbert
      public static void Main(string[] args)
      {
         Console.WriteLine("Dilbert Image Downloader");

         ArrayList arguments = new ArrayList(args);

         List<(string Key, string Value)> tokenizedArgs = args.Select(arg => arg.Split('=')).Select(keyAndValue => (keyAndValue[0], keyAndValue[1])).ToList();

         string arg_count = GetArgValue(tokenizedArgs, "count");
         bool downloadAllImages = arg_count == "all";
         int numberOfImagesToDownload = int.TryParse(arg_count, out numberOfImagesToDownload) ? numberOfImagesToDownload : 1;

         bool autoClose = bool.TryParse(GetArgValue(tokenizedArgs, "autoclose"), out autoClose) ? autoClose : false;

         string arg_savefolder = GetArgValue(tokenizedArgs, "savefolder");
         string saveFolder = string.IsNullOrWhiteSpace(arg_savefolder) || arg_savefolder.EndsWith('\\')
                             ? arg_savefolder
                             : arg_savefolder + "\\";

         string arg_readingfolder = GetArgValue(tokenizedArgs, "readingfolder");
         string readingFolder = string.IsNullOrWhiteSpace(arg_readingfolder) || arg_readingfolder.EndsWith('\\')
                             ? arg_readingfolder
                             : arg_readingfolder + "\\";

         if (string.IsNullOrWhiteSpace(saveFolder))
         {
            Console.WriteLine("Must pass a value for savefolder.");
            return;
         }

         for (int i = 0; i < numberOfImagesToDownload || downloadAllImages; i++)
         {
            (DateTime date,
                string formattedDate,
                string foldername,
                string foldernameForReading,
                string filename,
                string filenameForReading) = GetNextDayToSave(saveFolder, readingFolder);

            if (date > DateTime.Today)
            {
               break;
            }

            EnsureDestinationFoldersExist(foldername, readingFolder, foldernameForReading);

            string page = GetImagePage(formattedDate);

            if (TryGetImageUrl(page, out string imageUrl))
            {
               Console.Write("Downloading " + new FileInfo(filename).Name + " ...");
               new WebClient().DownloadFile(imageUrl, filename);
               if (!string.IsNullOrWhiteSpace(readingFolder))
               {
                  File.Copy(filename, filenameForReading);
               }
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

         if (!autoClose)
         {
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
         }
      }

      private static string GetArgValue(List<(string Key, string Value)> tokenizedArgs, string key)
      {
         return tokenizedArgs.SingleOrDefault(arg => arg.Key == key).Value;
      }

      private static
            (DateTime date,
            string formattedDate,
            string foldername,
            string foldernameForReading,
            string filename,
            string filenameForReading) GetNextDayToSave(string saveFolder, string readingFolder)
      {
         DateTime date = StartDate.AddDays(-1);
         string formattedDate;
         string foldername;
         string foldernameForReading;
         string filename;
         string filenameForReading;

         do
         {
            date = date.AddDays(1);
            formattedDate = date.ToString("yyyy-MM-dd");
            foldername = saveFolder + date.Year + @"\";
            foldernameForReading = readingFolder + date.Year + @"\";
            filename = foldername + SaveNameFormat.Replace("{date}", formattedDate);
            filenameForReading = foldernameForReading + SaveNameFormat.Replace("{date}", formattedDate);
         }
         while (File.Exists(filename));

         return (date, formattedDate, foldername, foldernameForReading, filename, filenameForReading);
      }

      private static void EnsureDestinationFoldersExist(string foldername, string readingFolder, string foldernameForReading)
      {
         Directory.CreateDirectory(foldername);
         if (!string.IsNullOrWhiteSpace(readingFolder))
         {
            Directory.CreateDirectory(foldernameForReading);
         }
      }

      private static string GetImagePage(string formattedDate)
      {
         return new HttpClient().GetStringAsync(BaseUrl + formattedDate).Result;
      }

      private static bool TryGetImageUrl(string page, out string imageUrl)
      {
         Regex imageUrlRegex = new Regex(ImageUrlRegex);
         Match imageUrlMatch = imageUrlRegex.Match(page);
         imageUrl = ImageBaseUrl + imageUrlMatch.Groups[1].Value;
         return imageUrlMatch.Success;
      }
   }
}
