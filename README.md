# LinuxServerDataminerPOC - Dokumentacija projekta

Ovaj projekat predstavlja Proof of Concept (POC) rješenje razvijeno u .NET 10 okviru za praćenje sistemskih metrika Linux servera i njihovo izlaganje u JSON i XML formatima radi integracije sa Skyline DataMiner Cube platformom.

Aplikacija je dizajnirana po principima čiste arhitekture (Clean Architecture) i SOLID principa, pri čemu su svi slojevi organizovani unutar jednog .NET projekta radi jednostavnosti i lakšeg održavanja.

---

## 1. Arhitektura projekta i struktura foldera

Struktura projekta je podijeljena po logičkim cjelinama (slojevima):

```text
LinuxServerDataminerPOC/
├── Domain/
│   ├── Entities/
│   │   └── ServerMetrics.cs
│   └── Interfaces/
│       └── ILinuxMetricsCollector.cs
├── Application/
│   ├── Dtos/
│   │   └── ServerMetricsDto.cs
│   ├── Options/
│   │   └── MetricsOptions.cs
│   ├── Interfaces/
│   │   └── IMetricsService.cs
│   └── Services/
│       └── MetricsService.cs
├── Infrastructure/
│   ├── Collectors/
│   │   └── RealLinuxMetricsCollector.cs
│   └── Health/
│       └── ServerHealthCheck.cs
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── Program.cs
└── LinuxServerDataminerPOC.csproj
```

---

## 2. Detaljno objašnjenje slojeva i fajlova

### Domain sloj (Domen)

Domain sloj predstavlja srce aplikacije i ne zavisi niti od jedne vanjske biblioteke ili tehnologije.

* **Domain/Entities/ServerMetrics.cs**
  * **Uloga:** Entitet koji definira osnovne sirove podatke o stanju servera (CPU procenat, ukupna i iskorištena memorija u MB, procenat diska, uptime i vremenska oznaka).
  * **Zašto radi:** Koristi C# `record` tip radi nepromjenjivosti (immutability) i sigurnosti obrade podataka.

* **Domain/Interfaces/ILinuxMetricsCollector.cs**
  * **Uloga:** Interfejs koji definiše ugovor za prikupljanje metrika sa servera (`CollectMetricsAsync`).
  * **Zašto radi:** Primjenjuje Dependency Inversion Principle (DIP). Aplikacija zavisi od ove apstrakcije, a ne od konkretne implementacije čitača metrika.

---

### Application sloj (Aplikativni sloj)

Application sloj sadrži biznis logiku, upravljanje konfiguracijom i definicije struktura za prenos podataka.

* **Application/Options/MetricsOptions.cs**
  * **Uloga:** Klasa koja mapira postavke iz `appsettings.json` (interval osvježavanja, pragi upozorenja za CPU, režim simulacije).
  * **Zašto radi:** Implementira Options Pattern (`IOptions<MetricsOptions>`), što omogućava centralizovano upravljanje konfiguracijom.

* **Application/Dtos/ServerMetricsDto.cs**
  * **Uloga:** Data Transfer Object (DTO) koji se šalje klijentima (DataMiner Cube). Sadrži izračunate vrijednosti kao što su procenat iskorištenosti memorije i tekstualni status sistema (OK / WARNING).
  * **Zašto radi:** Pored parametarskog konstruktora, posjeduje i prazan podrazumijevani konstruktor neophodan za `XmlSerializer` prilikom generisanja XML odgovora.

* **Application/Interfaces/IMetricsService.cs**
  * **Uloga:** Interfejs koji definiše servisne metode za dobavljanje prerađenih metrika (`GetCurrentMetricsAsync`).

* **Application/Services/MetricsService.cs**
  * **Uloga:** Glavni aplikativni servis koji prima sirove podatke od `ILinuxMetricsCollector`, preračunava procente i primjenjuje poslovna pravila iz `MetricsOptions`.
  * **Zašto radi:** Odvaja čitanje sirovih podataka od njihove interpretacije. Ako je CPU iznad definisanog praga, postavlja status na "WARNING".

---

### Infrastructure sloj (Infrastruktura)

Infrastructure sloj sadrži konkretne tehnološke implementacije za čitanje sistemskih fajlova i provjeru zdravlja aplikacije.

* **Infrastructure/Collectors/RealLinuxMetricsCollector.cs**
  * **Uloga:** Implementacija `ILinuxMetricsCollector` interfejsa koja čita stvarne podatke o RAM-u i CPU-u direktno sa Linux operativnog sistema.
  * **Kako radi:**
    * Prvo provjerava da li postoji `/host/proc` (ako se aplikacija izvršava unutar Docker kontejnera sa mapiranim volumenom) ili klasični `/proc`.
    * RAM memoriju čita iz virtuelnog fajla `/proc/meminfo` (parsiranjem `MemTotal` i `MemAvailable`).
    * CPU procenat računa iz `/proc/stat` mjerenjem razlike u `idle` i `total` vremenima u razmaku od 100 milisekundi.
    * Ukoliko se aplikacija pokrene na okruženju koje nema `/proc` fajlove, automatski se aktivira sigurni fallback mehanizam kako aplikacija ne bi pukla.

* **Infrastructure/Health/ServerHealthCheck.cs**
  * **Uloga:** Implementacija standardnog .NET `IHealthCheck` interfejsa.
  * **Kako radi:** Prilikom poziva `/health` endpointa, dobavlja trenutne metrike. Ako je CPU iznad 95%, vraća status `Unhealthy`, ako je iznad 80%, vraća `Degraded`, u suprotnom vraća `Healthy`.

---

### API i Konfiguracija

* **appsettings.json**
  * Sadrži postavke za Serilog strukturirano logovanje i prage upozorenja u `MetricsOptions` sekciji.

* **Program.cs**
  * **Uloga:** Composition Root aplikacije.
  * **Kako radi:**
    * Inicijalizuje Serilog loger koji piše strukturirane logove u konzolu.
    * Registruje `MetricsOptions`, `RealLinuxMetricsCollector`, `MetricsService` i Health Checks u Dependency Injection kontejner.
    * Izlaže tri ključna endpointa:
      1. `GET /api/metrics` - Vraća JSON format.
      2. `GET /api/metrics/xml` - Vraća XML format namijenjen DataMineru.
      3. `GET /health` - Vraća status zdravlja servisa.

---

## 3. Uputstvo za lokalno testiranje

1. **Pokretanje aplikacije:**
   U terminalu unutar projekta izvršiti komandu:
   ```bash
   dotnet run
   ```
   Aplikacija će se pokrenuti na `http://localhost:5051` (lokalno) ili putem Cloudflare Tunnel-a na `https://dataminerpoc.hodzicmirza.com`.

2. **Testiranje JSON endpointa:**
   ```bash
   curl -s http://localhost:5051/api/metrics
   # ili putem javne domene:
   curl -s https://dataminerpoc.hodzicmirza.com/api/metrics
   ```
   Očekivani izlaz (JSON):
   ```json
   {
     "cpuUsagePercentage": 22.5,
     "totalMemoryMb": 11704,
     "usedMemoryMb": 9790,
     "memoryUsagePercentage": 83.65,
     "diskUsagePercentage": 44.38,
     "systemStatus": "OK",
     "timestampUtc": "2026-07-27T18:44:45.6720596Z"
   }
   ```

3. **Testiranje XML endpointa (za DataMiner):**
   ```bash
   curl -s http://localhost:5051/api/metrics/xml
   # ili putem javne domene:
   curl -s https://dataminerpoc.hodzicmirza.com/api/metrics/xml
   ```
   Očekivani izlaz (XML):
   ```xml
   <?xml version="1.0" encoding="utf-16"?>
   <ServerMetricsDto xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
     <CpuUsagePercentage>22.5</CpuUsagePercentage>
     <TotalMemoryMb>11704</TotalMemoryMb>
     <UsedMemoryMb>9790</UsedMemoryMb>
     <MemoryUsagePercentage>83.65</MemoryUsagePercentage>
     <DiskUsagePercentage>44.38</DiskUsagePercentage>
     <SystemStatus>OK</SystemStatus>
     <TimestampUtc>2026-07-27T18:45:06.9927976Z</TimestampUtc>
   </ServerMetricsDto>
   ```

4. **Testiranje Health Check endpointa:**
   ```bash
   curl -s http://localhost:5051/health
   # ili putem javne domene:
   curl -s https://dataminerpoc.hodzicmirza.com/health
   ```
   Očekivani izlaz: `Healthy`.

---

## 4. Koraci za integraciju u DataMiner Cube

Da bi se ovaj Web API povezao sa Skyline DataMiner Cube platformom, potrebno je u DataMineru napraviti odgovarajući protokol (konektor).

### Korak 1: Kreiranje DataMiner Protocol XML-a
U DataMiner Studio-u kreira se novi Protocol XML fajl sa HTTP konekcijom.

### Korak 2: Definisanje parametara (Params)
U fajlu se definišu Parameter ID-jevi (PID) za svaku metriku:
* `Param ID 101`: CPU Usage (Analog, %)
* `Param ID 102`: Memory Usage (Analog, %)
* `Param ID 103`: System Status (Read, String)

### Korak 3: Definisanje HTTP sesije i XPath parsiranja
U `<HTTP>` sekciji protokola definiše se periodični `GET` zahtjev prema `/api/metrics/xml`:

```xml
<Protocol xmlns="http://www.skyline.be/config/protocol">
  <Params>
    <Param id="101" trending="true">
      <Name>CpuUsagePercentage</Name>
      <Type>read</Type>
      <Interprete><Type>double</Type></Interprete>
      <Display><RTDisplay>true</RTDisplay><Units>%</Units></Display>
    </Param>

    <Param id="102" trending="true">
      <Name>MemoryUsagePercentage</Name>
      <Type>read</Type>
      <Interprete><Type>double</Type></Interprete>
      <Display><RTDisplay>true</RTDisplay><Units>%</Units></Display>
    </Param>

    <Param id="103">
      <Name>SystemStatus</Name>
      <Type>read</Type>
      <Interprete><Type>string</Type></Interprete>
      <Display><RTDisplay>true</RTDisplay></Display>
    </Param>
  </Params>

  <HTTP>
    <Session id="1" connection="http" timer="5000">
      <Request verb="GET">
        <URL>/api/metrics/xml</URL>
      </Request>
      <Response>
        <Content format="xml">
          <Param id="101" xpath="/ServerMetricsDto/CpuUsagePercentage" />
          <Param id="102" xpath="/ServerMetricsDto/MemoryUsagePercentage" />
          <Param id="103" xpath="/ServerMetricsDto/SystemStatus" />
        </Content>
      </Response>
    </Session>
  </HTTP>
</Protocol>
```

### Korak 4: Dodavanje elementa u DataMiner Cube
1. Otvoriti DataMiner Cube interfejs.
2. Dodati novi Element i odabrati kreirani protokol.
3. Za adresu bus veze unijeti domen `dataminerpoc.hodzicmirza.com` na portu `443` (HTTPS) ili lokalno `localhost:5051`.
4. DataMiner će automatski svakih 5 sekundi preuzimati XML podatke, prikazivati ih na kartici elementa i crtati grafike u realnom vremenu.

---

## 5. Demonstracija i Video Prikaz (Live Demo)

Video prezentacija rada servisa, prikaza XML odgovora u realnom vremenu i integracije sa DataMiner Cube interfejsom ugrađena je direktno u nastavku (fajl se nalazi u repozitoriju pod `assets/demo.mp4`):

<video src="assets/demo.mp4" controls width="100%" poster="assets/thumbnail.png">
  Vaš preglednik ne podržava direktnu reprodukciju videa. Možete ga preuzeti direktno iz repozitorija na stazi: assets/demo.mp4
</video>

* Direktna veza na video u repozitoriju: [assets/demo.mp4](assets/demo.mp4)


