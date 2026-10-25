# Yapay Zekâ Kullanım Günlüğü — G6

**Araç:** Claude Code (model: Claude Opus 5.5), Unity Editor'a komut satırı (batchmode) üzerinden bağlı.
**Kullanım düzeyi:** 2. düzey, Editor'a bağlı ajan. Yapay zekâ dosya yazdı, Unity'yi komut satırından çalıştırdı, derleme günlüğünü okudu, ekran görüntüsü alıp kendi çıktısını denetledi.
**Doğrulama:** Her adımda Unity derlemesi (0 hata), Windows deneme derlemesi ve otomatik ekran görüntüleri.

> Ekip notu: Bu günlüğü kendi istemlerinizle sürdürün. Sunumda en az üç istemi ve aşağıdaki hatalardan birini canlı gösterin.

---

## İstemler

### İstem 1 — Görev tanımı
> Masaüstündeki AR_Uygulama_Gelistirme_Izlencesi dosyasını incele. Biz Grup 6'yız (G6). Bu projeyi Unity ile geliştir. Basit yapma, güzel ve şaşırtıcı olsun.

**Sonuç:** Yapay zekâ izlencede G6 için iki iş buldu (5. hafta anlatımı ve dönem projesi). Hangisinin istendiğini sordu. Android'de ARCore'un nesne takibi desteklemediğini belirtip teknoloji seçimini de sordu.

### İstem 2 — Seçimler
> 5. hafta anlatımı. Vuforia Model Target. Basit bir nesne olursa daha iyi olur.

**Sonuç:** Zımba makinesi seçildi: her sınıfta var, kutu biçimli (kolay modellenir), asimetrik (önde örs ve basma başlığı, arkada menteşe), içinde patlatılmış görünümde gösterilecek gizli parçalar (tel dizisi, itici yay) var.

### İstem 3 — Mimari (yapay zekânın kendi planı, ekip onayladı)
> Uygulama Vuforia'ya doğrudan bağlı olmasın; Vuforia yokken de editörde çalışsın. Takip kaynağını soyutla, simülasyon kaynağı yaz. Vuforia kodu yalnızca paket varken derlensin.

**Sonuç:** `TrackingSource` soyut sınıfı, `SimulatedTrackingSource`, `VuforiaTrackingSource` ve `G6.Vuforia.asmdef` içinde `versionDefines` + `defineConstraints`.

### İstem 4 — Görsel kalite
> Etiketler ekrandan taşıyor ve üst üste biniyor. Kartları ekranın iki kenarında sütunlara diz, çakışmayı çöz.

**Sonuç:** `LabelManager.LayoutColumn`: kartlar yukarıdan aşağı ve aşağıdan yukarı iki geçişte itilerek çakışmasız dizildi.

### İstem 5 — Vuforia'yı uçtan uca kurma
> Vuforia kısmını hallet. Hesabı açtım. Zımba 12 cm, mavi; fotoğraflar masaüstünde.

**Sonuç:** Yapay zekâ Vuforia paketini projeye ekledi, Model Target CLI'yı indirdi, zımbanın 3B modelini ölçülerden üretip DAE + JSON olarak dışa aktardı ve AR sahnesini kurdu. Hesap şifresini kendisi girmedi: betik (`Tools/ModelTargetUret.ps1`) şifreyi kullanıcıya kendi penceresinde sordu. Lisans anahtarını da ekip üyesi kendisi girdi.

### İstem 6 — Nesne değişikliği
> Tam üstüne oturmuyor ve sadece o açıdan tanıyor. Daha basit bir nesne için yapabilir miyiz? Elimde Arduino Mega 2560 var.

**Sonuç:** Zımba yerine Arduino Mega seçildi. Yapay zekâ Arduino'nun resmî PCB tasarım dosyasını (`.brd`) indirip bileşen konumlarını oradan okudu. Model milimetre hassasiyetinde üretildi.

### İstem 7 — Profesyonel arayüz ve "şaşırt"
> Arayüzdeki yazıları kaldır, silüet çıkmasın, kamera Arduino'yu gördüğünde olaylar gerçekleşsin. Pinler çok yakın ve karışık. Tam bir AR uygulaması yap, beni şaşırt.

**Sonuç:** Yazısız arayüz (yalnızca tarama halkası ve araç çubuğu), yalnızca 7 ana bölgenin etiketlendiği sade görünüm, 92 pinlik harita, akım animasyonu, sanal Blink ve canlı kod paneli. Silüetsiz tanıma için kılavuz açıları arka planda döndürüldü, üstten fotoğraftan bir Image Target eklendi.

### İstem 8 — Fotoğraf hedefi
> İstediğin fotoğrafı masaüstüne kaydettim.

**Sonuç:** Yapay zekâ kart kenarlarını OpenCV ile buldu, fotoğrafı perspektif düzeltmesiyle 101,6 × 53,34 mm oranına kırptı, uygulamaya ekledi, APK'yı derleyip telefona kurdu ve ekran görüntüsüyle hologramın gerçek karta oturduğunu doğruladı.

---

## Yapay zekânın yaptığı hatalar ve düzeltmeleri

### Hata 1 — Simülasyonda nesne hiç "bulunmuyor"

**Belirti:** Ekran görüntüsünde 5 saniye sonra da "Zımba aranıyor…" yazıyordu.

**Kod (hatalı):**
```csharp
void Start() { startTime = Time.time; manualLost = true; }
void Update()
{
    if (manualLost && startTime > 0f && Time.time - startTime > autoFindDelay) { ... }
}
```
**Neden:** `Start` ilk karede çalışır ve o anda `Time.time == 0`. Bu yüzden `startTime > 0f` hiçbir zaman doğru olmaz. Yapay zekâ "bekleyen iş var mı" bilgisini zaman değişkeninin işaretine gömmüştü.

**Düzeltme:** Ayrı bir `autoFindPending` bayrağı kullanıldı. Ders: Durumu bir sayının özel değerine (0, -1) gömmek yerine açık bir bool kullanın.

### Hata 2 — TextMeshPro kaynakları içe aktarılmadı

**Belirti:** Kurulum betiği "TMP Essential Resources içe aktarıldı" yazdı, ama `Assets/TextMesh Pro` klasörü oluşmadı.

**Neden:** `AssetDatabase.ImportPackage` asenkron çalışır. Unity `-batchmode -quit` ile çalışınca içe aktarma bitmeden kapandı. Günlük mesajı yanıltıcıydı.

**Düzeltme:** `.unitypackage` bir tar.gz arşividir. Arşiv elle açılıp dosyalar `Assets/` altına yazıldı. Editörde menüden çalıştırıldığında `ImportPackage` sorunsuz çalışır.

### Hata 3 — Etiket yazıları kayboldu

**Belirti:** Masa yüzeyi kartları kesiyordu. Kartlar kameraya 10 cm yaklaştırılınca bu kez yazılar tamamen kayboldu, yalnızca boş kartlar kaldı.

**Neden:** Dünya uzayındaki kartın ölçeği yaklaşık 0.00005'e düştü. TextMeshPro bu kadar küçük ölçekte metni çizemedi.

**Düzeltme:** Kartlar Screen Space Overlay canvas'a taşındı (her zaman üstte, net). Yalnızca çizgi ve nokta dünya uzayında kaldı. Kart dokunuşları fizik ışını yerine `Button.onClick` ile alındı.

### Hata 4 — Quiz'de bazı parçalara dokunulamıyor (tasarım hatası)

**Belirti:** "Zımba Teli nerede?" sorusunda tel dizisine dokunmak imkânsızdı.

**Neden:** Tel dizisi ve yay şarjörün içinde. Fizik ışını önce dıştaki şarjörün collider'ına çarpıyor.

**Düzeltme:** Quiz başlarken patlatılmış görünüm otomatik açılır.

### Hata 5 — Android'de "SphereCollider doesn't exist"

**Belirti:** Uygulama telefonda açıldı ama hologram hiç görünmedi. Günlükte: `Can't add component because class 'SphereCollider' doesn't exist!`

**Neden:** IL2CPP derlemesi kullanılmayan sınıfları siler (code stripping). `SphereCollider` yalnızca koddan `AddComponent` ile eklendiği için Unity onu kullanılmıyor sandı.

**Düzeltme:** `Assets/G6/link.xml` ile `SphereCollider` ve `CapsuleCollider` korundu. Ders: Editörde çalışan her şey cihazda çalışmaz; cihaz günlüğünü (`adb logcat`) okuyun.

### Hata 6 — Koddan oluşturulan Model Target'ta kılavuz görünmedi

**Belirti:** Zımba kameraya gösterildi, hiçbir şey olmadı.

**Neden:** Çalışma anında `CreateModelTarget` ile oluşturulan hedefte kılavuz görünüm modu varsayılan olarak kapalıydı. Kullanıcı hangi açıdan bakacağını bilmiyordu.

**Düzeltme:** Önce `GuideViewMode = GuideView2D` yapıldı. Sonra kullanıcı silüet istemeyince açılar görünmeden sırayla denenecek şekilde değiştirildi ve Image Target eklendi.

### Hata 7 — Hologram zımbanın üstüne tam oturmadı (model hatası)

**Belirti:** Takip başladı, ama hologram gerçek zımbadan birkaç milimetre kaymıştı.

**Neden:** Yapay zekâ zımbanın modelini birkaç cetvel ölçüsü ve fotoğraftan tahmin etti. Yuvarlak hatlar ve eğimler modelde yoktu. Model Target modelin gerçek nesneye uymasına çok duyarlıdır.

**Düzeltme:** Ölçüleri kesin bilinen bir nesneye (Arduino Mega, resmî PCB dosyası) geçildi. Ders: Nesne seçimi, kod kadar önemlidir.

### Hata 8 — Vuforia 11 ve Android minimum API

**Belirti:** Android derlemesi başarısız oldu.

**Neden:** Vuforia Engine 11, Android 10 (API 29) ve üstünü ister. Proje API 24'e ayarlıydı.

**Düzeltme:** `ProjectSetup.cs` minimum API'yi 29 yapacak şekilde güncellendi.

---

## Konuyla ilgili sık yapılan hata (sunumda gösterilecek)

**Türkçe bölge ayarında OBJ dışa aktarma:** `string.Format("{0}", 0.5f)` Türkçe Windows'ta `0,5` üretir. Model Target Generator bu dosyayı okuyamaz. `ObjExporter` bu yüzden `CultureInfo.InvariantCulture` kullanır.
