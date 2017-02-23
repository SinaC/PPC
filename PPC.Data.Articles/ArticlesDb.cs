﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using PPC.DataContracts;

namespace PPC.Data.Articles
{
    public class ArticlesDb
    {
        #region Singleton

        private static readonly Lazy<ArticlesDb> Lazy = new Lazy<ArticlesDb>(() => new ArticlesDb(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static ArticlesDb Instance => Lazy.Value;

        private ArticlesDb()
        {
            // TODO: ideally Load should be called here but if an exception occurs in Load, it will not bubble
        }

        #endregion

        private List<Article> _articles;
        public IEnumerable<Article> Articles => _articles;

        public void Add(Article article)
        {
            _articles.Add(article);
            Save();
        }

        public void ImportFromCsv(string filename)
        {
            //string filename = @"C:\temp\ppc\liste des produits.csv";
            if (File.Exists(filename))
            {
                int newArticlesCount = 0;
                int categoryModifiedCount = 0;
                int priceModifiedCount = 0;
                int supplierPriceModifiedCount = 0;
                int vatModifiedCount = 0;
                string[] lines = File.ReadAllLines(filename, Encoding.GetEncoding("iso-8859-1"));
                // column 2: id
                // column 3: description
                // column 6: category
                // column 10: supplier price
                // column 13: price
                // column 16: price-vat -> can be used to compute vat
                // if column 0 or 1 is non-empty or 3 is empty -> irrevelant line
                foreach (string rawLine in lines)
                {
                    string[] tokens = SplitCsv(rawLine).ToArray();
                    if (string.IsNullOrWhiteSpace(tokens[0]) && string.IsNullOrWhiteSpace(tokens[1]) && !string.IsNullOrWhiteSpace(tokens[3]))
                    {
                        Debug.Assert(tokens.Length == 17);
                        string id = tokens[2];
                        string description = tokens[3];

                        bool isNewArticle = false;

                        // search if article already exists
                        Article article = Articles.SingleOrDefault(x => x.Ean == id && x.Description.Trim().ToLowerInvariant() == description.ToLowerInvariant());
                        if (article == null)
                        {
                            article = new Article
                            {
                                Guid = Guid.NewGuid(),
                                Ean = id,
                                Description = description,
                            };
                            _articles.Add(article);
                            isNewArticle = true;
                        }

                        string category = tokens[6].Trim();
                        int stock;
                        if (!int.TryParse(tokens[8], out stock))
                            stock = 0;
                        decimal supplierPrice;
                        if (!decimal.TryParse(tokens[10], out supplierPrice))
                            supplierPrice = 0;
                        decimal price;
                        if (!decimal.TryParse(tokens[13], out price))
                            price = 0;
                        decimal priceNoVat;
                        if (!decimal.TryParse(tokens[16], out priceNoVat))
                            priceNoVat = 0;
                        decimal vat = Math.Round(100 * (price - priceNoVat) / priceNoVat, 0, MidpointRounding.AwayFromZero);
                        VatRates vatRate = vat == 6 ? VatRates.FoodDrink : VatRates.Other;

                        if (isNewArticle)
                        {
                            Debug.WriteLine($"NEW: Id:[{id}] Descr:[{description}] Cat:[{category}] P:[{price:C}] SP:[{supplierPrice:C}] VAT:[{vatRate}] Stock:[{stock}] PHT:[{priceNoVat}]");
                            newArticlesCount++;
                        }
                        else
                        {
                            if (!string.Equals(category.Trim(), article.Category.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                Debug.WriteLine($"{id} {description}: category: [{article.Category}] -> [{category}]");
                                categoryModifiedCount++;
                            }
                            if (price != article.Price)
                            {
                                Debug.WriteLine($"{id} {description}: price: [{article.Price:C}] -> [{price:C}]");
                                priceModifiedCount++;
                            }
                            if (supplierPrice != article.SupplierPrice)
                            {
                                Debug.WriteLine($"{id} {description}: supplierPrice: [{article.SupplierPrice:C}] -> [{supplierPrice:C}]");
                                supplierPriceModifiedCount++;
                            }
                            if (vatRate != article.VatRate)
                            {
                                Debug.WriteLine($"{id} {description}: VatRate: [{article.VatRate}] -> [{vatRate}]");
                                vatModifiedCount++;
                            }
                        }

                        article.Category = category;
                        article.Price = price;
                        article.SupplierPrice = supplierPrice;
                        article.Stock = stock;
                        article.VatRate = vatRate;
                    }
                }

                Debug.WriteLine($"New: {newArticlesCount}");
                Debug.WriteLine($"Category modified: {categoryModifiedCount}");
                Debug.WriteLine($"Price modified: {priceModifiedCount}");
                Debug.WriteLine($"SupplierPrice modified: {supplierPriceModifiedCount}");
                Debug.WriteLine($"VAT modified: {vatModifiedCount}");

                //if (newArticlesCount > 0 || categoryModifiedCount > 0 || priceModifiedCount > 0 || supplierPriceModifiedCount > 0 || vatModifiedCount > 0)
                //    Save();
            }
        }

        public void Save()
        {
            string filename = ConfigurationManager.AppSettings["ArticlesPath"];
            using (XmlTextWriter writer = new XmlTextWriter(filename, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<Article>));
                serializer.WriteObject(writer, Articles);
            }
        }

        public void Load()
        {
            // Load articles
            string filename = ConfigurationManager.AppSettings["ArticlesPath"];
            if (File.Exists(filename))
            {
                List<Article> newArticles;
                using (XmlTextReader reader = new XmlTextReader(filename))
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(List<Article>));
                    newArticles = (List<Article>)serializer.ReadObject(reader);
                }
                _articles = newArticles;
            }
            else
                throw new InvalidOperationException("Article DB not found.");
        }

        private static readonly Regex CsvSplitRegEx = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)", RegexOptions.Compiled);

        private static IEnumerable<string> SplitCsv(string input)
        {
            foreach (Match match in CsvSplitRegEx.Matches(input))
                yield return match.Value.TrimStart(',').TrimStart('\"').TrimEnd('\"').Trim();
        }
    }
}