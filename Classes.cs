using System;
using System.Collections.Generic;

    public class DayRange
    {
        public string from ;
        public string to ;
    }

    public class HourRange
    {
        public string from ;
        public string to ;
    }

    public class SearchData
    {
        public DayRange dayRange = new DayRange() ;
        public HourRange hourRange = new HourRange();
        public string prescriptionId ;
        public string voiId ;
        public string geoId ;
        public List<string> vaccineTypes;
        public string servicePointid = null;
    }
    public class ServicePointSearch {
        public string voiId ;
        public string geoId ;
    }

public class VisitsRoot {
    public List<VisitToBook> list = new List<VisitToBook>();
}

    public class ServicePoint
    {
        public string id ;
        public string name ;
        public string addressText ;
        public string mobility ;
    }

    public class VisitToBook
    {
        public string id ;
        public DateTime startAt ;
        public int duration ;
        public ServicePoint servicePoint ;
        public string vaccineType ;
        public string mobility ;
        public int dose ;
        public string status ;
    }

    public class Config {
        public Boolean _EXP_BookMode = false;
        public string PushOverUserId = null; // https://pushover.net/
        public string PushOverAppTokenId = null; // https://pushover.net/
        public int MinimalnaIloscTerminowWTymSamymMiejscuICzasie = 20; 
        public string DataOd = "2021-04-25";
        public string DataDo = "2021-04-26";
        public string GeoID = null; //warszawa, znajdz swoje na https://eteryt.stat.gov.pl/eTeryt/rejestr_teryt/udostepnianie_danych/baza_teryt/uzytkownicy_indywidualni/wyszukiwanie/wyszukiwanie.aspx?contrast=default
        public string WojewodztwoID = null; //dwie pierwsze cyfry GeoID
        public string NumerTelefonu = null;
        public List<string> Szczepionki =null;
        public string GodzinaOd = "0:01";
        public string GodzinaDo = "23:59";
        public string PunktID =null;
        public int CoIleSekundSprawdzac = 120;
        public string TelegramBotAccessToken = null;
        public bool VerbooseLogging =  false;
        public bool WojewodztwoJesliNiemaWMiescie = true;
        public bool WszystkieSzczepionkiJesliBrakZFiltra = false;
        
    }

