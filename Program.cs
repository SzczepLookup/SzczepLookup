using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Args;
using Titanium.Web.Proxy;



namespace Pacjent_PLay
{
    class Program
    {
        private static TelegramBotClient botClient;
        private static string messagePrefix = "";
        //private static string TelegramMessageSuffix = "";
        private static string TelegramMessageSuffix = "";
        private static string message = "Startuję program";
        private static Config cfg = new Config();
        private static Boolean RunMe = true;
        public static string SkierowanieID = "";
        public static string DataWygasnieciaSkierowania = "2021-08-31";
        public static DateTime LastSlotDate = DateTime.Now;
        public static List<KeyValuePair<string, string>> ExtraHeaders = new List<KeyValuePair<string, string>>();
        private static string DoRequest(string url, string RequestBody, HttpMethod method = null, List<KeyValuePair<string, string>> ExtraHeaders = null)
        {
            string result = "";
            HttpClient h = new HttpClient();
            h.Timeout = TimeSpan.FromSeconds(120);
            //h.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            HttpRequestMessage request = new HttpRequestMessage(method, url);
            if (ExtraHeaders != null)
            {
                foreach (var item in ExtraHeaders)
                {
                    request.Headers.Add(item.Key, item.Value);
                }
            }
            if (RequestBody != "")
            {
                if (cfg.VerbooseLogging) { Console.WriteLine(RequestBody); }
                request.Content = new StringContent(RequestBody);
                var MediaType = new MediaTypeHeaderValue("application/json");
                MediaType.CharSet = "utf-8";
                request.Content.Headers.ContentType = MediaType;
            }
            try
            {
                HttpResponseMessage response = h.SendAsync(request).Result;
                result = response.Content.ReadAsStringAsync().Result;
                if (cfg.VerbooseLogging) { Console.WriteLine(result); }
            }
            catch
            {
            }
            return result;
        }

        public static void SendPushMessage(string UserId, string AppTokenId, string MessageTitle, string MessageText)
        {
            var parameters = new NameValueCollection {
                { "token", AppTokenId },
                { "user", UserId },
                { "message", MessageText},
                { "title", MessageTitle },
                { "sound", "bike" }
};

            var client = new WebClient();
            client.UploadValues("https://api.pushover.net/1/messages.json", parameters);

        }

        private static void DoCapture()
        {
            //Tuple<Titanium.Web.Proxy.Http.HttpWebClient, string> Output = null;
            do
            {
                try
                {
                    var h = new HttpCapturer();
                    var Output = h.WaitForCaptureAndReturnOutput("pacjent.erejestracja.ezdrowie.gov.pl/api/patient", "Odśwież przeglądarke : https://pacjent.erejestracja.ezdrowie.gov.pl/wizyty. Jeśli podano numer telefonu w config.json sprawdz czy profil w portalu jest zgodny z numerem telefonu ", cfg.NumerTelefonu);
                    dynamic d = JsonConvert.DeserializeObject<dynamic>(Output.Item2);
                    //SkierowanieID = d.appointments[0].prescriptionId.Value;
                    SkierowanieID = d.prescriptions[0].id.Value;
                    DataWygasnieciaSkierowania = ((DateTime)d.prescriptions[0].expiringAt.Value).ToString("yyyy-MM-dd");
                    ExtraHeaders.Clear();
                    ExtraHeaders.Add(new KeyValuePair<string, string>("X-Csrf-Token", Output.Item1.Request.Headers.Where(h => h.Name == "X-Csrf-Token").FirstOrDefault().Value));
                    ExtraHeaders.Add(new KeyValuePair<string, string>("Cookie", Output.Item1.Request.Headers.Where(h => h.Name == "Cookie").FirstOrDefault().Value));
                }
                catch { }
                System.Threading.Thread.Sleep(500);
            } while (SkierowanieID == "");

        }

        public static List<VisitToBook> SendSearchRequest(string DateFrom, string DateMax, string WojewodztwoID, string GeoID, string SkierowanieID, List<VisitToBook> Lista, List<string> Szczepionki = null, string PunktId = null)
        {
            string RequestBody = "";
            SearchData searchData = new SearchData();
            searchData.dayRange.from = DateFrom;
            searchData.dayRange.to = DateMax;
            searchData.hourRange.from = cfg.GodzinaOd;
            searchData.hourRange.to = cfg.GodzinaDo;
            searchData.geoId = GeoID;
            searchData.voiId = WojewodztwoID;
            searchData.prescriptionId = SkierowanieID;
            searchData.servicePointid = PunktId;
            searchData.vaccineTypes = Szczepionki;
            RequestBody = JsonConvert.SerializeObject(searchData, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            string result = DoRequest("https://pacjent.erejestracja.ezdrowie.gov.pl/api/calendarSlots/find", RequestBody, HttpMethod.Post, ExtraHeaders);
            dynamic d = JsonConvert.DeserializeObject<dynamic>(result);
            if (d.errorCode != null)
            {
                if (d.errorCode.Value == "ERR_UNAUTHORIZED")
                {
                    if (cfg.PushOverUserId != null)
                    {
                        message = "Czekam na zalogowanie do portalu szczepien";
                        SendPushMessage(cfg.PushOverUserId, cfg.PushOverAppTokenId, "Covid-szczepienia", "Zaloguj się do szczepien");
                    }

                    Console.WriteLine("Sesja wygasła. Zaloguj się na https://pacjent.erejestracja.ezdrowie.gov.pl/wizyty i wciśnij enter");
                    Console.ReadLine();
                    DoCapture();
                    result = DoRequest("https://pacjent.erejestracja.ezdrowie.gov.pl/api/calendarSlots/find", RequestBody, HttpMethod.Post, ExtraHeaders);

                }
            }
            List<VisitToBook> d2 = JsonConvert.DeserializeObject<VisitsRoot>(result).list;
            if (Lista != null)
            {
                for (int i = 0; i < d2.Count; i++)
                {
                    Lista.Add(d2[i]);
                }
            }
            else
            {
                Lista = d2;
            }
            string json = JsonConvert.SerializeObject(Lista, Formatting.Indented);
            System.IO.File.WriteAllText("kolejne.json", json);
            return Lista;
        }
        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                Console.WriteLine("====> Telegram : " + e.Message.Chat.Username + " " + e.Message.Chat.FirstName + " " + e.Message.Chat.LastName + ": " + e.Message.Text);

                await botClient.SendTextMessageAsync(
                  chatId: e.Message.Chat,
                  disableWebPagePreview: true,
                  text: message + "   " + TelegramMessageSuffix
                );
            }
        }

        static void Main(string[] args)
        {
            string ConfigFileName = "config.json";
            if (args.Count() == 1)
            {
                ConfigFileName = args[0];
            }
            cfg = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText(ConfigFileName));
            if (DateTime.Parse(cfg.DataOd).Date < DateTime.Now)
            {
                cfg.DataOd = DateTime.Now.ToString("yyyy-MM-dd");
            }

            if (cfg.TelegramBotAccessToken != null)
            {
                botClient = new TelegramBotClient(cfg.TelegramBotAccessToken);
                botClient.OnMessage += Bot_OnMessage;
                botClient.StartReceiving();
                message = "Z powodu zmian w portalu szczepień, osoby po 1szej dawce nie mogą używać API do szukania wizyt, więc nie mam jak ich pokazać. Sprawdź, orientacyjne daty na https://szczepienia.github.io/" + System.Environment.NewLine + System.Environment.NewLine + "Pomyśl nad upolowaniem 1szego terminu dla siebie i uruchom program: https://github.com/SzczepLookup/SzczepLookup";
            }
            //RunMe = false;
            //Console.ReadLine();
            if (RunMe)
            {
                Console.WriteLine("Odśwież przeglądarce: https://pacjent.erejestracja.ezdrowie.gov.pl/wizyty  , zaloguj się (jeśli trzeba) i wciśnij enter");
                //Console.WriteLine();
                Console.ReadLine();

                DoCapture();
            }
            do
            {
                try { Search(); } catch { message = "Błąd pobierania danych"; Console.WriteLine(message); }
                System.Threading.Thread.Sleep(cfg.CoIleSekundSprawdzac * 1000);
            }
            while (RunMe);
            if (botClient != null)
            {
                botClient.StopReceiving();
            }
        }
        private static void Search()
        {
            //messagePrefix = "W MIEŚCIE ";
            List<VisitToBook> terminy_wszystkie_szczep = null;
            List<VisitToBook> TerminySzczepienia = SendSearchRequest(cfg.DataOd, cfg.DataDo, cfg.WojewodztwoID, cfg.GeoID, SkierowanieID, null, cfg.Szczepionki, cfg.PunktID);
            if (!cfg._EXP_BookMode & 1 == 0)
            {
                ServicePointSearch SPS = new ServicePointSearch();
                SPS.voiId = cfg.WojewodztwoID;
                SPS.geoId = cfg.GeoID;
                string RequestBody = JsonConvert.SerializeObject(SPS, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                terminy_wszystkie_szczep = SendSearchRequest(cfg.DataOd, DataWygasnieciaSkierowania, cfg.WojewodztwoID, cfg.GeoID, SkierowanieID, null, null, cfg.PunktID);
                string result = DoRequest("https://pacjent.erejestracja.ezdrowie.gov.pl/api/servicePoints/find", RequestBody, HttpMethod.Post, ExtraHeaders);
                System.IO.File.WriteAllText("servicePoints.json", result);
                TerminySzczepienia = SendSearchRequest(cfg.DataOd, DateTime.Parse(cfg.DataOd).AddDays(30).ToString("yyyy-MM-dd"), cfg.WojewodztwoID, cfg.GeoID, SkierowanieID, TerminySzczepienia, cfg.Szczepionki, cfg.PunktID);
                TerminySzczepienia = SendSearchRequest(cfg.DataOd, DataWygasnieciaSkierowania, cfg.WojewodztwoID, cfg.GeoID, SkierowanieID, TerminySzczepienia, cfg.Szczepionki, cfg.PunktID);
                if (TerminySzczepienia.Count > 0)
                {
                    var DateMaxFound = TerminySzczepienia.Max(dd => dd.startAt);
                    TerminySzczepienia = SendSearchRequest(DateMaxFound.AddDays(1).ToString("yyyy-MM-dd"), DateMaxFound.AddDays(30).ToString("yyyy-MM-dd"), cfg.WojewodztwoID, cfg.GeoID, SkierowanieID, TerminySzczepienia, cfg.Szczepionki, cfg.PunktID);
                }
                if (TerminySzczepienia.Count == 0 & cfg.WojewodztwoJesliNiemaWMiescie)
                {
                    //messagePrefix = "W WOJEWÓDZTWIE ";
                    TerminySzczepienia = SendSearchRequest(cfg.DataOd, DateTime.Parse(cfg.DataOd).AddDays(28).ToString("yyyy-MM-dd"), cfg.WojewodztwoID, null, SkierowanieID, TerminySzczepienia, cfg.Szczepionki, cfg.PunktID);
                }
                if (TerminySzczepienia.Count == 0 & cfg.WszystkieSzczepionkiJesliBrakZFiltra)
                {
                    //messagePrefix = "W WOJEWÓDZTWIE ";
                    TerminySzczepienia = SendSearchRequest(cfg.DataOd, DateTime.Parse(cfg.DataOd).AddDays(28).ToString("yyyy-MM-dd"), cfg.WojewodztwoID, cfg.GeoID, SkierowanieID, TerminySzczepienia, null, cfg.PunktID);
                }

                if (TerminySzczepienia.Count == 0 & cfg.WojewodztwoJesliNiemaWMiescie & cfg.WszystkieSzczepionkiJesliBrakZFiltra)
                {
                    //messagePrefix = "W WOJEWÓDZTWIE ";
                    TerminySzczepienia = SendSearchRequest(cfg.DataOd, DateTime.Parse(cfg.DataOd).AddDays(28).ToString("yyyy-MM-dd"), cfg.WojewodztwoID, null, SkierowanieID, TerminySzczepienia, null, cfg.PunktID);
                }
            }
            if (TerminySzczepienia.Count == 0)
            {
                if (terminy_wszystkie_szczep == null)
                {
                    message = DateTime.Now.ToShortTimeString() + " Brak terminow na (" + String.Join(",", cfg.Szczepionki) + ") na tą chwilę";
                }
                else
                {
                    var assigments2 = terminy_wszystkie_szczep.OrderBy(o => o.startAt).Distinct().ToList();
                    message = DateTime.Now.ToShortTimeString() + " Brak terminow na (" + String.Join(",", cfg.Szczepionki) + ") na tą chwilę"; 
                }
                Console.WriteLine(message);
            }
            else
            {
                var assigments = TerminySzczepienia.OrderBy(o => o.startAt).Distinct().ToList();
                List<string> ls = new List<string>();
                for (int i = 1; (i < assigments.Count & i <= 10); i++)
                {
                    ls.Add(((DateTime)assigments[i].startAt.ToLocalTime()).ToString());
                }
                var visit = assigments[0];


                if (cfg._EXP_BookMode)
                {
                    string RequestBody2 = @"{""prescriptionId"": """ + SkierowanieID + @"""}";
                    string result = DoRequest(String.Format("https://pacjent.erejestracja.ezdrowie.gov.pl/api/calendarSlot/{0}/confirm", visit.id), RequestBody2, HttpMethod.Post, ExtraHeaders);
                    Console.WriteLine(result);
                    RunMe = false;
                    if (cfg.PushOverUserId != null)
                    {
                        SendPushMessage(cfg.PushOverUserId, cfg.PushOverAppTokenId, "Covid-szczepienia", "Sprawdz czy próba rezerwacji terminu powiodła się");
                        Console.WriteLine("==> Push wyslany");
                    }
                }

                if ((visit.startAt).Date <= DateTime.Parse(cfg.DataDo).Date)
                {
                    var similarAssignments = assigments.Where(ass => visit.startAt.Date == ass.startAt.Date & visit.servicePoint.name == ass.servicePoint.name).ToList();
                    if (similarAssignments.Count >= cfg.MinimalnaIloscTerminowWTymSamymMiejscuICzasie)
                    {
                        if (botClient == null)
                        {
                            message = DateTime.Now.ToShortTimeString() + " " + messagePrefix + visit.startAt.ToLocalTime() + " " + visit.vaccineType + " " + visit.servicePoint.name + " " + visit.servicePoint.addressText;
                            Console.WriteLine("======> " + message);
                            if (LastSlotDate != visit.startAt)
                            {
                                LastSlotDate = visit.startAt;
                                if (cfg.PushOverUserId != null)
                                {
                                    SendPushMessage(cfg.PushOverUserId, cfg.PushOverAppTokenId, "Covid-szczepienia", message);
                                    Console.WriteLine("==> Push wyslany");
                                }
                            }
                        }
                        else
                        {
                            message = DateTime.Now.ToShortTimeString() + " " + messagePrefix + visit.startAt.ToLocalTime() + " " + visit.vaccineType + " " + visit.servicePoint.name + " " + visit.servicePoint.addressText + ", Kolejne (miejsca mogą być inne): " + String.Join("/", ls.ToArray());
                            Console.WriteLine(message);
                        }

                    }
                    else
                    {

                        if (botClient == null)
                        {
                            message = DateTime.Now.ToShortTimeString() + " " + messagePrefix + visit.startAt.ToLocalTime() + " " + visit.vaccineType + " " + visit.servicePoint.name + " " + visit.servicePoint.addressText + ", Kolejne (miejssca mogą być inne): " + String.Join("/", ls.ToArray());
                            Console.WriteLine(message);
                        }
                        else
                        {
                            message = "Za mało terminów: " + messagePrefix + DateTime.Now.ToShortTimeString() + " " + visit.startAt.ToLocalTime() + " " + visit.vaccineType + " " + visit.servicePoint.name + " " + (string)visit.servicePoint.addressText + ", PO WSKAZANEJ DACIE!. Kolejne: " + String.Join("/", ls.ToArray());
                            Console.WriteLine(message);
                        }
                    }
                }
                else
                {
                    if (botClient == null)
                    {
                        message = DateTime.Now.ToShortTimeString() + " " + messagePrefix + visit.startAt.ToLocalTime() + " " + visit.vaccineType + " " + visit.servicePoint.name + " " + visit.servicePoint.addressText + ", PO WSKAZANEJ DACIE!. Kolejne: " + String.Join("/", ls.ToArray());
                    }
                    else
                    {
                        message = DateTime.Now.ToShortTimeString() + " " + messagePrefix + visit.startAt.ToLocalTime() + " " + visit.vaccineType + " " + visit.servicePoint.name + " " + visit.servicePoint.addressText + ", Kolejne (miejca mogą być inne): " + String.Join("/", ls.ToArray());
                        Console.WriteLine(message);
                    }
                }
            }
        }

    }
}

