# DNSGame

یک اپلیکیشن متن‌باز ویندوز برای ارزیابی endpointهای شبکه، اندازه‌گیری Ping، Jitter و Packet Loss، تشخیص DNSهای واقعاً پاسخ‌گو و اعمال امن DNS روی کارت شبکه.

> [!IMPORTANT]
> تغییر DNS به‌تنهایی مسیر فیزیکی اینترنت را کوتاه نمی‌کند و نمی‌تواند جایگزین VPN یا تونل اختصاصی شود. DNSGame فقط endpointی را قابل اعمال می‌داند که واقعاً روی UDP/53 پاسخ DNS معتبر بدهد. Routing بازی از طریق یک سرور دیگر، به WireGuard/Wintun یا یک gateway تحت کنترل نیاز دارد و در نسخه اولیه عمداً شبیه‌سازی نشده است.

## قابلیت‌ها

- تست موازی بیش از 100 IP و hostname
- نمایش میانگین Ping، Jitter، Packet Loss و زمان پاسخ DNS
- resolve کردن hostnameها و نمایش IPv4 نهایی
- امتیازدهی ترکیبی برای مرتب‌سازی endpointها
- جلوگیری از اعمال IPهایی که DNS معتبر نیستند
- تشخیص کارت‌های شبکه فعال ویندوز
- ذخیره تنظیمات DNS قبلی پیش از تغییر
- بازگردانی دقیق تنظیمات قبلی یا DHCP
- اجرای self-contained روی Windows x64 بدون نیاز به نصب .NET
- Installer مستقل و pipeline خودکار GitHub Actions

## مدل امتیازدهی

امتیاز کمتر بهتر است:

```text
Score = Average Ping + (2 × Jitter) + (4 × Packet Loss %) + DNS response penalty
```

Jitter برابر میانگین اختلاف مطلق بین RTTهای موفق متوالی است. Packet Loss از تعداد probeهای بی‌پاسخ محاسبه می‌شود. ICMP ممکن است توسط بعضی سرورها مسدود شود؛ بنابراین `Unreachable` لزوماً به معنی خاموش بودن میزبان نیست.

## معماری

```text
DNSGame.App (WPF / Windows)
├── Persian desktop UI
├── Adapter discovery
└── Windows DNS apply / snapshot / rollback
        │
        ▼
DNSGame.Core (.NET 8)
├── Endpoint parsing and validation
├── ICMP probe
├── UDP/53 DNS protocol probe
├── Metrics and scoring
└── Concurrent benchmark orchestration
```

هسته به ویندوز وابسته نیست و جداگانه unit test می‌شود. بخش تغییر تنظیمات شبکه فقط در پروژه WPF قرار دارد و دستورات PowerShell را به‌صورت Base64 EncodedCommand اجرا می‌کند تا ورودی shell تزریق‌پذیر نباشد.

## اجرا از سورس

پیش‌نیازها:

- Windows 10/11 x64
- .NET 8 SDK
- PowerShell 5.1 یا جدیدتر

```powershell
git clone https://github.com/leonardo0231/DNSGame.git
cd DNSGame
dotnet restore
dotnet test
dotnet run --project src/DNSGame.App
```

برنامه برای تغییر DNS دسترسی Administrator درخواست می‌کند.

## ساخت Installer

GitHub Actions در هر push به `main` مراحل format check، unit test، publish ویندوز و ساخت Installer با Inno Setup را اجرا می‌کند. خروجی با نام `DNSGame-Windows-x64-Installer` در Artifacts همان workflow قرار می‌گیرد.

برای انتشار نسخه رسمی:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

با push شدن tag، Installer به GitHub Releases نیز پیوست می‌شود.

## محدودیت نسخه 0.1

- endpointهای ورودی عمومی و خارج از کنترل پروژه‌اند؛ در دسترس بودن یا امنیت آن‌ها تضمین نمی‌شود.
- برنامه در نسخه فعلی DNS سیستم را بهینه می‌کند و کیفیت endpointها را می‌سنجد؛ تونل ترافیک بازی ارائه نمی‌دهد.
- برای routing واقعی مرحله بعدی، یک provider مبتنی بر WireGuard و سرورهای اختصاصی لازم است.

## امنیت و بازیابی

پیش از اولین تغییر، DNS فعلی در `%LocalAppData%\DNSGame\dns-snapshot.json` ذخیره می‌شود. دکمه «بازگردانی DNS» همان snapshot را اعمال و cache ویندوز را پاک می‌کند. اگر snapshot وجود نداشته باشد، تنظیمات DNS کارت شبکه به حالت خودکار برمی‌گردد.

## وضعیت پروژه

- [x] هسته تست Ping/Jitter/Loss
- [x] تست واقعی DNS روی UDP/53
- [x] رابط WPF فارسی
- [x] اعمال و rollback تنظیمات DNS
- [x] unit tests و CI ویندوز
- [x] ساخت Installer خودکار
- [ ] provider تونل WireGuard با سرور اختصاصی
- [ ] پروفایل‌های جداگانه برای Fortnite و League of Legends
- [ ] تست TCP/UDP روی پورت‌های واقعی بازی

## License

تا زمان انتخاب مجوز نهایی، تمام حقوق پروژه متعلق به صاحب مخزن است.
