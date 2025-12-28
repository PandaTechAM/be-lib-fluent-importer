namespace FluentImporter.Tests;

public class ServiceTests
{
   private readonly List<Dictionary<string, string>> data =
   [
      new()
      {
         {
            "Id".ToLower(), "1"
         },
         {
            "Name".ToLower(), "Name1"
         },
         {
            "Description".ToLower(), "Description1"
         },
         {
            "Date".ToLower(), "2021-01-01"
         },
         {
            "Comment".ToLower(), "Comment1"
         }
      },

      new()
      {
         {
            "Id".ToLower(), "3"
         },
         {
            "Name".ToLower(), "Name1"
         },
         {
            "Description".ToLower(), "Description1"
         },
         {
            "Date".ToLower(), "2021-01-01"
         },
         {
            "Comment".ToLower(), "Comment1"
         }
      },

      new()
      {
         {
            "Id".ToLower(), "4"
         },
         {
            "Name".ToLower(), "Name1"
         },
         {
            "Description".ToLower(), "Description1"
         },
         {
            "Date".ToLower(), "2021-01-01"
         },
         {
            "Comment".ToLower(), "Comment1"
         }
      }
   ];


   [Fact]
   public async Task Import_Data()
   {
      var rule = new FileDataImportRule();
      var records = rule.GetRecords(data);

      Assert.Equal(3, records.Count());
   }
}