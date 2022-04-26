using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using System.Threading.Tasks;
using CsvHelper.Configuration;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var cookie = Path.Combine(Environment.CurrentDirectory, "test.cookie"); Куки в формате - NetScape
            const string BASE_URL = "https://www.toy.ru";
            string CSV_PATH = Path.Combine(Environment.CurrentDirectory, "itog.csv");
            IConfiguration CONFIG = Configuration.Default
                .WithJs()
                .WithDefaultLoader();
                //.WithPersistentCookies(cookie) //не работает, а только грузит
            using IBrowsingContext CONTEXT = BrowsingContext.New(CONFIG);
            //context.SetCookie(new Url(BASE_URL), "BITRIX_SM_city=77000000000"); //попытка принудительной установки нужной куки - неудачна
            using IDocument doc = await CONTEXT.OpenAsync($"{BASE_URL}/catalog/boy_transport/");
            //var b = doc.ExecuteScript("this.SaveGeoCity(77000000000)"); //попытка применить скрипт, который вызывается при смене города
            
            short pagesCount = 1;
            short count = (short)doc.GetElementsByClassName("page-link").Length;
            if (count != 0) pagesCount = Convert.ToInt16(doc.QuerySelector("nav>ul>li.page-item:nth-last-child(2)").TextContent);

            string[] urls = new string[pagesCount];
            for (int i = 1; i <= pagesCount; i++)
            {
                urls[i-1] = ($"{BASE_URL}/catalog/boy_transport/?filterseccode%5B0%5D=transport&PAGEN_8=" + i.ToString());
            }

            var docs = new List<Task<IDocument>>();
            foreach (var url in urls)
            {
                docs.Add(CONTEXT.OpenAsync(url));
                await Task.Delay(200);
            }
            Task.WaitAll(docs.ToArray());
            docs.ForEach(p => p.Dispose());

            var ItemsUrl = new List<string>();
            foreach (var t in docs)
            {
                t.Result.QuerySelectorAll("div.col-12 > a.gtm-click.p-1[href]").ToList().ForEach(p => ItemsUrl.Add(BASE_URL + p.GetAttribute("href")));
                Console.WriteLine($"Парсинг страницы - {t.Result.Title}");
            }
            docs.Clear();


            var tasks = new List<Task>();
            List<ItemData> items = new List<ItemData>();
            foreach (var url in ItemsUrl)
            {
                tasks.Add(GetData(url));
                await Task.Delay(200);
            }
            Task.WaitAll(tasks.ToArray());
            tasks.ForEach(p => p.Dispose());

            CsvConfiguration csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" };
            using (StreamWriter streamWriter = new StreamWriter(CSV_PATH, false, Encoding.UTF8))
            {
                using (CsvWriter csvWriter = new CsvWriter(streamWriter, csvConfig))
                {
                    await csvWriter.WriteRecordsAsync(items);
                }
            }
            Console.ReadKey();


            async Task GetData(string url)
            {
                using (var _context = await CONTEXT.OpenAsync(url))
                {
                    try
                    {
                        string name = _context.GetElementsByClassName("detail-name")[0].TextContent;
                        string region = doc.QuerySelector("div.select-city-link>a").TextContent.Trim(new char[] { ' ', '\t', '\n' });
                        Console.WriteLine("- " + name);
                        string uri = _context.BaseUri;
                        List<string> navList = new List<string>();
                        _context.QuerySelectorAll("a.breadcrumb-item").ToList().ForEach(p => navList.Add(p.TextContent));
                        string navigation = navList.Aggregate((current, next) => current + "/" + next) + "/" + _context.QuerySelector("nav.breadcrumb > span.breadcrumb-item").TextContent;
                        string isActual = "Нет в наличии";
                        float oldPrice = 0;
                        float newPrice = 0;
                        List<string> photos = new List<string>();
                        if (_context.GetElementsByClassName("net-v-nalichii").Length != 1)
                        {
                            oldPrice = Convert.ToSingle(_context.GetElementsByClassName("old-price").Length > 0 ? _context.GetElementsByClassName("old-price")[0].TextContent.Replace(" руб.", "") : 0);
                            newPrice = Convert.ToSingle(_context.GetElementsByClassName("price")[0].TextContent.Replace(" руб.", ""));
                            isActual = "В наличии";
                            _context.QuerySelectorAll("div>img.img-fluid").ToList().ForEach(p => photos.Add(p.GetAttribute("src")));
                        }
                        ItemData itemData = new ItemData(region, navigation, oldPrice, newPrice, isActual, url, photos.Aggregate((current, next) => current + ", " + next));
                        items.Add(itemData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        _context.Dispose();
                    }
                }
            }
        }
    }
}
