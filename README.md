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

**Domain/Entities/ServerMetrics.cs**
- **Uloga:** Entitet koji definira osnovne sirove podatke o stanju servera (CPU procenat, ukupna i iskorištena memorija u MB, procenat diska, uptime i vremenska oznaka).
- **Zašto radi:** Koristi C# `record` tip radi nepromjenjivosti (immutability) i sigurnosti obrade podataka.

**Domain/Interfaces/ILinuxMetricsCollector.cs**
- **Uloga:** Interfejs koji definiše ugovor za prikupljanje metrika sa servera (`CollectMetricsAsync`).
- **Zašto radi:** Primjenjuje Dependency Inversion Principle (DIP). Aplikacija zavisi od ove apstrakcije, a ne od konkretne implementacije čitača metrika.

---

### Application sloj (Aplikativni sloj)

Application sloj sadrži biznis logiku, upravljanje konfiguracijom i definicije struktura za prenos podataka.

**Application/Options/MetricsOptions.cs**
- **Uloga:** Klasa koja mapira postavke iz `appsettings.json` (interval osvježavanja, pragi upozorenja za CPU, režim simulacije).
- **Zašto radi:** Implementira Options Pattern (`IOptions<MetricsOptions>`), što omogućava centralizovano upravljanje konfiguracijom.

**Application/Dtos/ServerMetricsDto.cs**
- **Uloga:** Data Transfer Object (DTO) koji se šalje klijentima (DataMiner Cube). Sadrži izračunate vrijednosti kao što su procenat iskorištenosti memorije i tekstualni status sistema (OK / WARNING).
- **Zašto radi:** Pored parametarskog konstruktora, posjeduje i prazan podrazumijevani konstruktor neophodan za `XmlSerializer` prilikom generisanja XML odgovora.

**Application/Interfaces/IMetricsService.cs**
- **Uloga:** Interfejs koji definiše servisne metode za dobavljanje prerađenih metrika (`GetCurrentMetricsAsync`).

**Application/Services/MetricsService.cs**
- **Uloga:** Glavni aplikativni servis koji prima sirove podatke od `ILinuxMetricsCollector`, preračunava procente i primjenjuje poslovna pravila iz `MetricsOptions`.
- **Zašto radi:** Odvaja čitanje sirovih podataka od njihove interpretacije. Ako je CPU iznad definisanog praga, postavlja status na "WARNING".

---

### Infrastructure sloj (Infrastruktura)

Infrastructure sloj sadrži konkretne tehnološke implementacije za čitanje sistemskih fajlova i provjeru zdravlja aplikacije.

**Infrastructure/Collectors/RealLinuxMetricsCollector.cs**
- **Uloga:** Implementacija `ILinuxMetricsCollector` interfejsa koja čita stvarne podatke o RAM-u i CPU-u direktno sa Linux operativnog sistema.
- **Kako radi:**
  - Prvo provjerava da li postoji `/host/proc` (ako se aplikacija izvršava unutar Docker kontejnera sa mapiranim volumenom) ili klasični `/proc`.
  - RAM memoriju čita iz virtuelnog fajla `/proc/meminfo` (parsiranjem `MemTotal` i `MemAvailable`).
  - CPU procenat računa iz `/proc/stat` mjerenjem razlike u `idle` i `total` vremenima u razmaku od 100 milisekundi.
  - Ukoliko se aplikacija pokrene na okruženju koje nema `/proc` fajlove, automatski se aktivira sigurni fallback mehanizam kako aplikacija ne bi pukla.

**Infrastructure/Health/ServerHealthCheck.cs**
- **Uloga:** Implementacija standardnog .NET `IHealthCheck` interfejsa.
- **Kako radi:** Prilikom poziva `/health` endpointa, dobavlja trenutne metrike. Ako je CPU iznad 95%, vraća status `Unhealthy`, ako je iznad 80%, vraća `Degraded`, u suprotnom vraća `Healthy`.

---

### API i Konfiguracija

**appsettings.json**
- Sadrži postavke za Serilog strukturirano logovanje i prage upozorenja u `MetricsOptions` sekciji.

**Program.cs**
- **Uloga:** Composition Root aplikacije.
- **Kako radi:**
  - Inicijalizuje Serilog loger koji piše strukturirane logove u konzolu.
  - Registruje `MetricsOptions`, `RealLinuxMetricsCollector`, `MetricsService` i Health Checks u Dependency Injection kontejner.
  - Izlaže tri ključna endpointa:
    1. `GET /api/metrics` - Vraća JSON format.
    2. `GET /api/metrics/xml` - Vraća XML format namijenjen DataMineru.
    3. `GET /health` - Vraća status zdravlja servisa.

---

## 3. Uputstvo za lokalno testiranje

### Pokretanje aplikacije

U terminalu unutar projekta izvršiti komandu:

```bash
dotnet run
```

Aplikacija će se pokrenuti na:
- **Lokalno:** `http://localhost:5051`
- **Javna domena:** `https://dataminercubepocsimulation.onrender.com`

### Testiranje JSON endpointa

```bash
curl -s http://localhost:5051/api/metrics
# ili putem javne domene:
curl -s https://dataminercubepocsimulation.onrender.com/api/metrics
```

**Očekivani izlaz (JSON):**

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

### Testiranje XML endpointa (za DataMiner)

```bash
curl -s http://localhost:5051/api/metrics/xml
# ili putem javne domene:
curl -s https://dataminercubepocsimulation.onrender.com/api/metrics/xml
```

**Očekivani izlaz (XML):**

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

### Testiranje Health Check endpointa

```bash
curl -s http://localhost:5051/health
# ili putem javne domene:
curl -s https://dataminercubepocsimulation.onrender.com/health
```

**Očekivani izlaz:** `Healthy`

---

## 4. Koraci za integraciju u DataMiner Cube

Da bi se ovaj Web API povezao sa Skyline DataMiner Cube platformom, potrebno je u DataMineru napraviti odgovarajući protokol (konektor).

### Korak 1: Kreiranje DataMiner Protocol XML-a

U DataMiner Studio-u kreira se novi Protocol XML fajl. Ispod je kompletan protokol koji treba da se iskoristi:

```xml
<Protocol xmlns="http://www.skyline.be/config/protocol">
  <Name>MirzaHodzicServerMonitoring</Name>
  <Version>1.0.1.0</Version>
  <Description>Protokol za monitoring .NET API servera</Description>
  <Vendor>Mirza Hodzic</Vendor>
  <ElementType>Http</ElementType>
  <Type>http</Type>
  <Provider>Mirza Hodzic</Provider>
  
  <Compliancies>
    <MinimumRequiredVersion>9.0.0.0</MinimumRequiredVersion>
  </Compliancies>

  <Display pageOrder="Performance" defaultPage="Performance" />

  <Params>
    <Param id="101" trending="true">
      <Name>CpuUsagePercentage</Name>
      <Description>CPU opterećenje servera</Description>
      <Type>read</Type>
      <Interprete><Type>double</Type><DefaultValue>0</DefaultValue></Interprete>
      <Display>
        <RTDisplay>true</RTDisplay>
        <Units>%</Units>
        <Decimals>2</Decimals>
        <Positions><Position><Page>Performance</Page><Row>0</Row><Column>0</Column></Position></Positions>
      </Display>
      <Alarm><Monitored>true</Monitored><WaH>70</WaH><MaH>85</MaH><CH>95</CH></Alarm>
    </Param>

    <Param id="102" trending="true">
      <Name>MemoryUsagePercentage</Name>
      <Description>Iskorištenost RAM-a (%)</Description>
      <Type>read</Type>
      <Interprete><Type>double</Type><DefaultValue>0</DefaultValue></Interprete>
      <Display>
        <RTDisplay>true</RTDisplay>
        <Units>%</Units>
        <Decimals>2</Decimals>
        <Positions><Position><Page>Performance</Page><Row>1</Row><Column>0</Column></Position></Positions>
      </Display>
      <Alarm><Monitored>true</Monitored><WaH>80</WaH><MaH>90</MaH><CH>95</CH></Alarm>
    </Param>

    <Param id="103">
      <Name>SystemStatus</Name>
      <Description>Status servera</Description>
      <Type>read</Type>
      <Interprete><Type>string</Type><DefaultValue>OK</DefaultValue></Interprete>
      <Display>
        <RTDisplay>true</RTDisplay>
        <Positions><Position><Page>Performance</Page><Row>2</Row><Column>0</Column></Position></Positions>
      </Display>
      <Alarm>
        <Monitored>true</Monitored>
        <Possible>
          <Value value="WARNING" severity="minor">Upozorenje</Value>
          <Value value="CRITICAL" severity="critical">Kritično</Value>
        </Possible>
      </Alarm>
    </Param>

    <Param id="104">
      <Name>TotalMemoryMb</Name>
      <Description>Ukupno RAM (MB)</Description>
      <Type>read</Type>
      <Interprete><Type>double</Type><DefaultValue>0</DefaultValue></Interprete>
      <Display>
        <RTDisplay>true</RTDisplay>
        <Units>MB</Units>
        <Positions><Position><Page>Performance</Page><Row>3</Row><Column>0</Column></Position></Positions>
      </Display>
    </Param>

    <Param id="105">
      <Name>UsedMemoryMb</Name>
      <Description>Iskorišteno RAM (MB)</Description>
      <Type>read</Type>
      <Interprete><Type>double</Type><DefaultValue>0</DefaultValue></Interprete>
      <Display>
        <RTDisplay>true</RTDisplay>
        <Units>MB</Units>
        <Positions><Position><Page>Performance</Page><Row>4</Row><Column>0</Column></Position></Positions>
      </Display>
    </Param>

    <Param id="106" trending="true">
      <Name>DiskUsagePercentage</Name>
      <Description>Iskorištenost diska (%)</Description>
      <Type>read</Type>
      <Interprete><Type>double</Type><DefaultValue>0</DefaultValue></Interprete>
      <Display>
        <RTDisplay>true</RTDisplay>
        <Units>%</Units>
        <Decimals>2</Decimals>
        <Positions><Position><Page>Performance</Page><Row>5</Row><Column>0</Column></Position></Positions>
      </Display>
    </Param>

    <Param id="107">
      <Name>TimestampUtc</Name>
      <Description>Vrijeme očitavanja (UTC)</Description>
      <Type>read</Type>
      <Interprete><Type>string</Type></Interprete>
      <Display>
        <RTDisplay>true</RTDisplay>
        <Positions><Position><Page>Performance</Page><Row>6</Row><Column>0</Column></Position></Positions>
      </Display>
    </Param>
  </Params>

  <Groups>
    <Group id="1">
      <Name>HTTP Polling Group</Name>
      <Type>poll</Type>
      <Content>
        <Session>1</Session>
      </Content>
    </Group>
  </Groups>

  <Timers>
    <Timer id="1" On="startup">
      <Name>5-Second Polling Timer</Name>
      <Time>5000</Time>
      <Interval>5000</Interval>
      <Content>
        <Group>1</Group>
      </Content>
    </Timer>
  </Timers>

  <HTTP>
    <Session id="1" connection="http">
      <Request verb="GET">
        <URL>/api/metrics/xml</URL>
      </Request>
      <Response>
        <Content format="xml">
          <Param id="101" xpath="/ServerMetricsDto/CpuUsagePercentage" />
          <Param id="102" xpath="/ServerMetricsDto/MemoryUsagePercentage" />
          <Param id="103" xpath="/ServerMetricsDto/SystemStatus" />
          <Param id="104" xpath="/ServerMetricsDto/TotalMemoryMb" />
          <Param id="105" xpath="/ServerMetricsDto/UsedMemoryMb" />
          <Param id="106" xpath="/ServerMetricsDto/DiskUsagePercentage" />
          <Param id="107" xpath="/ServerMetricsDto/TimestampUtc" />
        </Content>
      </Response>
    </Session>
  </HTTP>
</Protocol>
```

### Korak 2: Objašnjenje protokola

**Osnovne informacije:**
- **Name:** `MirzaHodzicServerMonitoring` - identifikator protokola
- **Version:** `1.0.1.0` - verzija protokola
- **ElementType:** `Http` - tip elementa koji će se kreirati
- **Display:** Prikazuje samo jednu stranicu "Performance" sa svim parametrima

**Parametri (Params):**
- **PID 101 (CPU):** Prikazuje CPU opterećenje sa alarmima na 70% (upozorenje), 85% (ozbiljno) i 95% (kritično)
- **PID 102 (RAM):** Prikazuje iskorištenost RAM-a sa alarmima na 80%, 90% i 95%
- **PID 103 (Status):** Tekstualni status sistema sa mogućim vrijednostima OK, WARNING, CRITICAL
- **PID 104 (Total RAM):** Ukupna količina RAM-a u MB
- **PID 105 (Used RAM):** Iskorištena količina RAM-a u MB
- **PID 106 (Disk):** Iskorištenost diska u procentima
- **PID 107 (Timestamp):** Vrijeme očitavanja u UTC formatu

**Poling (Polling):**
- Timer svakih 5 sekundi (5000ms) pokreće HTTP zahtjev
- HTTP GET zahtjev se šalje na `/api/metrics/xml`
- XML odgovor se parsira pomoću XPath izraza
- Svaki parametar dobija vrijednost iz odgovarajućeg XML elementa

### Korak 3: Dodavanje elementa u DataMiner Cube

1. Otvoriti DataMiner Cube interfejs.
2. Kliknuti na **"Add Element"** ili koristiti **CTRL+N**.
3. U polje **"Protocol"** pretražiti i odabrati **"MirzaHodzicServerMonitoring"**.
4. Za **"Name"** unijeti naziv elementa (npr. "Linux Server Monitor").
5. Za **"Address"** bus veze unijeti jednu od sljedećih adresa:
   - **Lokalno:** `http://localhost:5051`
   - **Javna domena:** `https://dataminercubepocsimulation.onrender.com`
6. Kliknuti **"Create"**.
7. Element će se pojaviti u listi elemenata i automatski početi prikupljati podatke.
8. Dvostrukim klikom na element otvara se prozor sa svim metrikama na "Performance" stranici.

### Korak 4: Vizuelizacija i monitoring

Nakon kreiranja elementa, DataMiner Cube će automatski:
- Prikazivati sve metrike u realnom vremenu na "Performance" tabu
- Generisati trendove (grafikone) za PID-ove sa `trending="true"` (CPU, RAM, Disk)
- Aktivirati alarme kada vrijednosti pređu definisane pragove
- Omogućiti kreiranje dashboarda sa prilagođenim prikazima

---

## 5. Demonstracija i Video Prikaz (Live Demo)

Video prezentacija rada servisa, prikaza XML odgovora u realnom vremenu i integracije sa DataMiner Cube interfejsom ugrađena je direktno u nastavku (fajl se nalazi u repozitoriju pod `assets/demo.mp4`):

<video src="assets/demo.mp4" controls width="100%" poster="assets/thumbnail.png">
  Vaš preglednik ne podržava direktnu reprodukciju videa. Možete ga preuzeti direktno iz repozitorija na stazi: assets/demo.mp4
</video>

**Direktna veza na video u repozitoriju:** [assets/demo.mp4](assets/demo.mp4)

---

## 6. Tehničke specifikacije

### Zahtjevi sistema
- **.NET 10** ili noviji
- **Linux** operativni sistem (za realno prikupljanje metrika)
- **Docker** (opciono, za kontejnerizaciju)

### Korištene tehnologije
- **ASP.NET Core 10** - Web API framework
- **Serilog** - Strukturirano logovanje
- **System.Xml.Serialization** - XML serijalizacija
- **Microsoft.Extensions.Diagnostics.HealthChecks** - Health check endpoint

### Portovi i endpointi
| Endpoint | Metoda | Format | Opis |
|----------|--------|--------|------|
| `/api/metrics` | GET | JSON | Sve metrike u JSON formatu |
| `/api/metrics/xml` | GET | XML | Sve metrike u XML formatu (za DataMiner) |
| `/health` | GET | JSON | Status zdravlja aplikacije |

---

## 7. Rješavanje problema (Troubleshooting)

### Aplikacija ne prikuplja metrike
- Provjeriti da li se aplikacija izvršava na Linux sistemu
- Provjeriti da li `/proc` direktorijum postoji i da li je čitljiv
- U Docker okruženju, provjeriti da li je volumen `/proc` mapiran

### DataMiner ne prima podatke
- Provjeriti da li je URL ispravan u DataMiner konfiguraciji
- Testirati XML endpoint ručno sa `curl`-om
- Provjeriti DataMiner logove za greške u parsiranju XML-a

### Health check vraća "Unhealthy"
- Provjeriti da li je CPU opterećenje preko 95%
- Provjeriti da li aplikacija ima pristup `/proc` fajlovima
- Restartovati aplikaciju

---

## 8. Budući razvoj i proširenja

- [ ] Dodavanje podrške za više servera (multi-tenancy)
- [ ] Implementacija WebSocket-a za real-time update
- [ ] Dodavanje autentifikacije (JWT)
- [ ] Proširenje metrika (network I/O, procesi, temperatura)
- [ ] Dodavanje Prometheus exporter-a
- [ ] Implementacija caching-a za smanjenje opterećenja

---

## 9. Licenca i kontakt

**Autor:** Mirza Hodžić  
**Projekat:** LinuxServerDataminerPOC  
**Verzija:** 1.0.0  
**Licenca:** MIT  

Za dodatne informacije, pitanja ili podršku, kontaktirajte autora putem GitHub repozitorija.

---

*Dokumentacija ažurirana: Juli 2026.*
