# SzczepLookup

1. Zainstalować **.net core SDK** z https://dotnet.microsoft.com/download . Windows/macOS/linux. Obecnie jest on w wersji 3.1 . Upewnij się że instalujesz **.NET Core** a nie .NET 5.0 lub .NET 6.0. 
2. Pobrać zawartość repozytorium (zielony przycisk => Download ZIP) i wypakowac lub sklonowac (git clone https://github.com/SzczepLookup/SzczepLookup)
3. Uruchomić linię poleceń (CMD/shell) i przejść do katalogu z wypakowanymi plikami (do tego gdzie jest plik program.cs i inne)
4. dotnet restore
5. Wyedytować plik config.json wedle preferencji (więcej info niżej)
6. W przegladarce zalogować się do https://pacjent.erejestracja.ezdrowie.gov.pl/wizyty . MUSI BYC JUZ WYSTAWIONE SKIEROWANIE NA SZCZEPIENIE!
7. dotnet run 
8. Wykonywać polecenia apki
9. Jak coś nie działa to CTRL-C, sprawdź czy ustawienia proxy są zgodne z oczekiwaniami i spróbuj ponownie od kroku 7

## Od 4.05.2021 ,  w związku z zmianami w Portalu , wyszukiwanie działa TYLKO dla osób, które nie dostały 1szej dawki. Po jej otrzymaniu, dostęp do API , dla "takich skierowań", jest zablokowany.  Ponieważ jestem już po pierwszej dawce, nie mam już jak testować tego programu :-( . Błędy naprawiam , jak ktoś mi je zgłosi :-) PRki też są, mile, widziane.


# config.json - składnia
* **_EXP_BookMode** - domyślnie **wyłączony** . Umożliwia automatyczne zarezerwowanie pierwszej, znalezionej, wizyty, która spełnia, zadane w konfiguracji, kryteria. Pamiętaj , że :
   - aby ta opcja zadziałała nie możesz mieć umówionej wizyty na szczepienie - drugiej nie zarezerwujesz a program nie anuluje istniejącej
   - w BookMode program kończy działanie wkrótce po próbie umówienia wizyty. Nie sprawda czy to się udało czy nie, ale wynik próby jest pokazany na ekranie. 
   - Jedna osoba przetestowała tą funkcjonalność i wizyta została umówiona! :-) 
* CoIleSekundSprawdzac - co ile sekund narzędzie ma szukać terminów
* PushOverUserId - jesli chcesz powiadomienia na telefon to podaje swoje dane z serwisu https://pushover.net
* PushOverAppTokenId - jesli chcesz powiadomienia na telefon to podaje swoje dane z serwisu https://pushover.net
* MinimalnaIloscTerminowWTymSamymMiejscuICzasie - ile visit ma szukać by dać powiadomienie. Proponuje od 10 wzwyż, chyba że chcesz skorzystać z automatycznego zarezerwowania wizyty wtedy ustaw na 1, aby oszczędzić czas . Wizyty znikają i dodają się w sekudny.
* DataOd - od kiedy ma szukać wizyt. Zachowaj ten format : 2021-04-30
* DataDo - do kiedy ma szukać wizyt. Zachowaj ten format : 2021-04-30
* GodzinaOd - od której godziny ma być wizyta , np 0:01
* GodzinaDo - do której godziny ma być wizyta , np 23:59
* GeoID - ID gminy. Znajdź go na https://bdl.stat.gov.pl/BDL/dane/teryt/jednostka . Jeśli ten wpis usuniesz to szukasz w całym województwie. Plik konfiguracyjny, który jest w tym repozytorium, ma wpisane ID Warszawy. Całej.  
* WojewodztwoID - dwie pierwsze cyfry powyższego. 
* NumerTelefonu - przydatne jeśli chcesz mieć uruchomioną więcej niż jedną instancję tego programu. Wtedy :
   - pierszą instancję uruchamiasz: dotnet run config.json
   - drugą i kolejne: dotnet run --no-build <inny_plik_konfiguracyjny>.json 
* Szczepionki - podaj jakie chcesz, skasuj jeśli obojętne. Dostępne opcje : cov19.pfizer, cov19.moderna, cov19.astra_zeneca , cov19.johnson_and_johnson
* PunktID - jeśli chcesz znaleźć termin w konkretnym punkcie , to podaj ID punktu. Żeby go znaleźć, użyj programu, zakończ jak zobaczysz wyniki i sprawdz plik servicePoints.json i znajdź tam ID punktu
* TelegramBotAccessToken - jeśli chcesz aby programik chodził w formie bota do Telegrama. Aby uzyskać własnego bota, napisz na Telegramie do BotFather . 
* VerbooseLogging - jeśli chcesz widzieć zapytania i odpowiedzi JSON do/z serwisu. Domyślnie wyłączone
* WojewodztwoJesliNiemaWMiescie - jeśli (nie)chcesz szukać w województwie, jeśli w mieście nie ma. Domyślnie włączone
* WszystkieSzczepionkiJesliBrakZFiltra - jeśli chcesz szukać wszyskitch szczepionek, jeśli nie znaleziono tych co chcesz. Domyślnie wyłączone
   - W przypadku ustawienia obu, powyższych opcji, szukanie w Województwie jest przeprowadzane przed szukaniem wszystkich szczepionek


Sesja logowanie do portalu Ezdrowie ma ważność (na dzień 1go maja) maksymalnie 12 godzin. Czasem mniej, jak Portal ma zmiany. Oznacza to, że program poprosi o ponowne zalogowanie do portalu , gdy sesja wygaśnie. s

Moje skierowanie traci ważność z końcem czerwca. Po tym dniu nie będę jak miał testować tego programu. 

POWODZENIA! Dawaj znać co i jak !, np tu: https://github.com/SzczepLookup/SzczepLookup/issues 
   
