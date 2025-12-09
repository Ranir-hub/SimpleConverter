using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Converter
{
    public class Currency
    {
        [JsonPropertyName("CharCode")]
        public string? CharCode { get; set; }
        [JsonPropertyName("Value")]
        public double Value { get; set; }
        public Currency(string charCode, double value) { CharCode = charCode; Value = value; }
    }

    public class Cbr
    {
        [JsonPropertyName("Valute")]
        public Dictionary<string, Currency> Valute { get; set; }
    }

    public static class GetCurrencies
    {
        public static async Task<(ObservableCollection<Currency>? Currencies, DateTime ActualDate)> ReadJson(DateTime date)
        {
            HttpClient client = new HttpClient();
            var currencies = new ObservableCollection<Currency>();
            while (currencies.Count == 0)
            {
                try
                {
                    string url;
                    if (date >= DateTime.Today)
                    {
                        url = $"https://www.cbr-xml-daily.ru/daily_json.js";
                        date = DateTime.Today;
                    }
                    else
                    {//https://www.cbr-xml-daily.ru/archive/2025/12/02/daily_json.js
                        url = $"https://www.cbr-xml-daily.ru/archive/{date.Year.ToString()}/{date.Month:D2}/{date.Day:D2}/daily_json.js";
                    }

                    using (var response = await client.GetAsync(url))
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        var res = JsonSerializer.Deserialize<Cbr>(body);
                        if (res != null)
                        {
                            foreach (var currency in res.Valute.Values) currencies.Add(currency);
                            currencies.Add(new Currency("RUB", 1));
                            return (currencies, date);
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    date = date.AddDays(-1);
                }
            }
            return (null, DateTime.Today);
        }
    }

    public class ConvertersViewModel : INotifyPropertyChanged
    {
        public ConvertersViewModel()
        {
            Load();
        }
        private DateTime date;
        public DateTime Date
        {
            get => date;
            set
            {
                if (date != value)
                {
                    date = value;
                    _ = LoadCurrencies();
                    OnPropertyChanged(nameof(Value2));
                    OnPropertyChanged(nameof(DateText));
                }
            }
        }
        public string DateText => date.ToString("D");

        private ObservableCollection<Currency> currencies;
        public ObservableCollection<Currency> Currencies
        {
            get => currencies;
            set
            {
                if (currencies != value)
                {
                    currencies = value;
                    OnPropertyChanged();
                }
            }
        }

        private double value1;
        public double Value1
        {
            get => value1;
            set
            {
                if (value1 != value)
                {
                    value1 = value;

                    if (SelectedIn != null && SelectedOut != null)
                    {
                        value2 = Math.Round(value1 * SelectedIn.Value / SelectedOut.Value, 6, MidpointRounding.ToEven);
                        OnPropertyChanged(nameof(Value2));
                    }
                }

            }
        }
        private double value2;
        public double Value2
        {
            get => value2;
            set
            {
                if (value2 != value)
                {
                    value2 = value;

                    if (SelectedIn != null && SelectedOut != null)
                    {
                        value1 = Math.Round(value2 / SelectedIn.Value * SelectedOut.Value, 6, MidpointRounding.ToEven);
                        OnPropertyChanged(nameof(Value1));
                    }
                }
            }
        }
        private Currency selectedIn;
        public Currency SelectedIn
        {
            get => selectedIn;
            set
            {
                if (selectedIn != value)
                {
                    selectedIn = value;
                    OnPropertyChanged();

                    if (SelectedIn != null && SelectedOut != null)
                    {
                        value2 = Math.Round(value1 * SelectedIn.Value / SelectedOut.Value, 6, MidpointRounding.ToEven);
                        OnPropertyChanged(nameof(Value2));
                    }
                }
            }
        }
        private Currency selectedOut;
        public Currency SelectedOut
        {
            get => selectedOut;
            set
            {
                if (selectedOut != value)
                {
                    selectedOut = value;
                    OnPropertyChanged();

                    if (SelectedIn != null && SelectedOut != null)
                    {
                        value2 = Math.Round(value1 * SelectedIn.Value / SelectedOut.Value, 6, MidpointRounding.ToEven);
                        OnPropertyChanged(nameof(Value2));
                    }
                }
            }
        }
        private async Task LoadCurrencies()
        {
            var result = await GetCurrencies.ReadJson(Date);
            Currencies = result.Currencies;
            Date = result.ActualDate;
            if (SelectedOut == null && SelectedIn == null)
            {
                SelectedOut = Currencies.FirstOrDefault(cc => cc.CharCode == Preferences.Get("SelectedOutCC", ""));
                SelectedIn = Currencies.FirstOrDefault(cc => cc.CharCode == Preferences.Get("SelectedInCC", ""));
            }
        }
        public void Load()
        {
            Date = Preferences.Get("Date", DateTime.Today);
            Value2 = Preferences.Get("Value2", Value2);
            Value1 = Preferences.Get("Value1", Value1);
        }
        public void Save()
        {
            Preferences.Set("Date", Date);
            Preferences.Set("Value1", Value1);
            Preferences.Set("Value2", Value2);
            Preferences.Set("SelectedInCC", (SelectedIn == null ? "" : SelectedIn.CharCode));
            Preferences.Set("SelectedOutCC", (SelectedOut == null ? "" : SelectedOut.CharCode));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
