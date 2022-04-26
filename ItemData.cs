using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    public class ItemData
    {
        public ItemData(string regionName, string navigationSource, float? oldPrice, float newPrice, string isActual, string url, string images = "")
        {
            RegionName = regionName;
            NavigationSource = navigationSource;
            OldPrice = oldPrice;
            NewPrice = newPrice;
            this.isActual = isActual;
            Images = images;
            Url = url;
        }

        [Name("Регион")]
        public string RegionName { get; set; }
        [Name("Хлебные крошки")]
        public string NavigationSource { get; set; }
        [Name("Старая цена")]
        public float? OldPrice { get; set; }
        [Name("Новая цена")]
        public float NewPrice { get; set; }
        [Name("Наличие товара")]
        public string isActual { get; set; }
        [Name("Ссылки на изображения")]
        public string Images { get; set; }
        [Name("Ссылка на товар")]
        public string Url { get; set; }
    }

    public class ItemDataMap : ClassMap<ItemData>
    {
        public ItemDataMap()
        {
            Map(m => m.RegionName).Index(0).Name("Регион");
            Map(m => m.NavigationSource).Index(1).Name("Хлебные крошки");
            Map(m => m.OldPrice).Index(2).Name("Старая цена");
            Map(m => m.NewPrice).Index(3).Name("Новая цена");
            Map(m => m.isActual).Index(4).Name("Наличие товара");
            Map(m => m.Images).Index(5).Name("Ссылки на изображения");
            Map(m => m.Url).Index(6).Name("Ссылка на товар");
        }
    }
}
