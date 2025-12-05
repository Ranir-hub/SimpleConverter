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
        public Dictionary<string, Currency>? Valute { get; set; }  
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
            Date = DateTime.Today;
        }
        private DateTime date;
        public DateTime Date
        {
            get => date;
            set
            {
                if(date != value)
                {
                    date = value;
                    DateText = date.ToString("D");
                    LoadCurrencies();
                    OnPropertyChanged();
                }
            }
        }
        private string datetext = DateTime.Today.ToString("D");
        public string DateText
        {
            get => datetext;
            set {
                if (datetext != date.ToString("D"))
                {
                    datetext = date.ToString("D");
                    OnPropertyChanged();
                }
            }
        }
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
        bool wasCalc = false;
        private double value1;
        public double Value1
        {
            get => value1;
            set
            {
                value1 = value;
                if (SelectedIn != null && SelectedOut != null && wasCalc == false)
                {
                    value2 = Math.Round(Value1 * SelectedIn.Value / SelectedOut.Value, 2, MidpointRounding.ToEven);
                    wasCalc = true;
                }
                OnPropertyChanged(nameof(Value2));
                wasCalc = false;
            }
        }
        private double value2;
        public double Value2
        {
            get => value2;
            set
            {
                value2 = value;
                if (SelectedIn != null && SelectedOut != null && wasCalc == false)
                {
                    value1 = Math.Round(Value2 / SelectedIn.Value * SelectedOut.Value, 2, MidpointRounding.ToEven);
                    wasCalc = true;
                }
                OnPropertyChanged(nameof(Value1));
                wasCalc = false;
            }
        }
        private Currency selectedIn;
        public Currency SelectedIn
        {
            get => selectedIn;
            set
            {
                selectedIn = value;
                OnPropertyChanged();
                if (SelectedIn != null && SelectedOut != null) value2 = Math.Round(Value1 * SelectedIn.Value / SelectedOut.Value, 2, MidpointRounding.ToEven);
                OnPropertyChanged(nameof(Value2));
            }
        }
        private Currency selectedOut;
        public Currency SelectedOut
        {
            get => selectedOut;
            set
            {
                selectedOut = value;
                OnPropertyChanged();
                if (SelectedIn != null && SelectedOut != null) value1 = Math.Round(Value2 / SelectedIn.Value * SelectedOut.Value, 2, MidpointRounding.ToEven);
                OnPropertyChanged(nameof(Value1));
            }
        }
        
        private async Task LoadCurrencies()
        {
            var result = await GetCurrencies.ReadJson(Date);
            Currencies = result.Currencies;
            Date = result.ActualDate;
            DateText = date.ToString("D");
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
