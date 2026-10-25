# Uygulama Föyü — G6: Nesne Takibi

**Hafta:** 5 · **Süre:** 40 dakika (≈13 dk anlatım + ≈27 dk canlı uygulama)
**Canlı uygulama:** Bir nesnenin (Arduino Mega 2560) parçalarını etiketleyen AR uygulaması.
**Önceki grup (G5, yüz takibi) ile bağ:** G4 görüntü takibini, G5 yüz takibini gösterdi. G6, takibi düz bir görselden veya yüzden **üç boyutlu katı bir nesneye** taşır.

---

## 1. Anlatım (13 dk)

### 1.1 Görüntü takibinden farkı

| | Görüntü takibi (Image Target) | Nesne takibi (Model Target) |
|---|---|---|
| Hedef | Düz, dokulu bir görsel | 3B katı nesne |
| Neyi tanır? | Görseldeki köşe ve doku noktalarını | Nesnenin **biçimini** (kenarlar, silüet) |
| Hedef nasıl üretilir? | Görsel yüklenir | 3B modelden (CAD/OBJ/DAE) üretilir |
| Desen gerekir mi? | Evet, bol detaylı doku | Hayır, düz renkli nesne de olur |
| Örnek | Afiş, kitap sayfası | Makine, ürün, elektronik kart |

AR Foundation'da nesne takibi yalnızca iOS'ta (ARKit) vardır. **ARCore nesne takibi sunmaz.** Bu yüzden Android için Vuforia Model Target kullanıyoruz.

### 1.2 Vuforia Model Target: 3B modelden hedef üretimi

1. Nesnenin **gerçek ölçülerde** 3B modeli gerekir.
2. Model, Model Target Generator (MTG) masaüstü uygulamasında ya da **Model Target CLI** ile işlenir. Biz CLI kullandık (`Tools/ModelTargetUret.ps1`): tek bir JSON dosyası modeli, yukarı yönü ve kılavuz açılarını tanımlar.
3. **Kılavuz görünüm (Guide View):** Kullanıcının nesneye hangi açıdan bakacağıdır. Klasik kullanımda bu açıdaki silüet ekranda gösterilir ve kullanıcı telefonu silüetle hizalar.
4. Çıktı bir veritabanıdır (`.xml` + `.dat`). `StreamingAssets/Vuforia/` altına konur.

**Bizim fark yaratan adımlarımız:**
- Kartın 3B modelini hazır bulmak yerine **koddan ürettik**. Bileşen konumları Arduino'nun resmî PCB dosyasından (`Arduino_MEGA_2560-Rev3.brd`) okundu. Aynı geometri hem hologram ikiz hem Model Target modeli olarak kullanılıyor; etiketler ve hologram, takip edilen modelle birebir aynı koordinatlarda.
- **Silüet göstermiyoruz.** Beş kılavuz açısı arka planda üçer saniyede bir sırayla etkinleştiriliyor (`SetActiveGuideViewName`).
- Yanına bir **Image Target** ekledik: kartın üstten çekilmiş, OpenCV ile düzeltilmiş fotoğrafı. Model Target belirli açılara bağlıyken fotoğraf hedefi kartı hemen her açıdan anında yakalıyor. İki hedef tek bir takip kaynağında birleşiyor (`VuforiaTrackingSource`).

### 1.3 Takibe uygun nesne seçimi

İlk denememiz bir **zımba makinesiydi**. Takip çalıştı, ama hologram tam oturmadı ve yalnızca kılavuz açısından tanıdı. Nedeni:
- Zımbanın yuvarlak hatlarını cetvelle ölçüp modellemek zor; model gerçek nesneden birkaç mm farklıydı.
- Mavi plastik gövde, düz ve az detaylı.

**Arduino Mega** çok daha iyi bir hedef:
- **Ölçüleri kesin biliniyor** (resmî PCB dosyası, 101,6 × 53,34 mm).
- **Asimetrik:** Bir uçta USB ve güç jakı, diğer uçta çift sıra pin.
- **Bol detaylı ve mat:** Image Target için ideal doku.
- **Katı ve hareketsiz.**

Zor nesneler:
- **Simetrik** (kupa, şişe, top): Yönü ayırt edilemez; poz döner veya titrer.
- **Parlak/yansıtıcı** (krom, cam): Görünüşü ışığa göre değişir, kenarlar kaybolur.
- **Esnek veya hareketli** (kablo, kumaş).
- **Modelden farklı** (ölçüler tutmuyor, klon kartta bileşen yerleri farklı).

### 1.4 Nesne üzerine bilgi etiketi yerleştirme

- Her parçanın bir **etiket noktası** var (`PartView.labelPoint`), parçanın yerel uzayında.
- Dijital ikiz takip çapasının **çocuğu** olduğu için Vuforia'nın verdiği pozla birlikte hareket eder.
- **Karmaşayı önleme:** 15 parçanın yalnızca 7'si "ana bölge" (`PartView.primary`). Diğerlerinin etiketi o parçaya dokununca açılır.
- Kartlar ekranın iki kenarına dizilir; çizgi, parçadaki noktadan karta gider (`LabelManager`). Kartlar ekran (overlay) katmanındadır; her mesafede aynı boyutta ve net okunur.
- **Pin etiketleri** ise kartın üstüne yatık, dünya uzayında duran TextMeshPro yazılarıdır (`PinMap`). Yazı yüksekliği gerçek ölçüde 1,6 mm'dir; 2,54 mm pin aralığına sığar ve pinler birbirine karışmaz.

### 1.5 Sınırlar ve lisans

- Model Target kalitesi, modelin gerçek nesneyle ölçü uyumuna bağlıdır.
- Basic plan, CLI ile sınırlı sayıda Model Target üretimine izin verir. **Advanced (360°) Model Target** CLI ile üretilemez.
- Ücretsiz lisansla Model Target kullanan uygulama ekranda **"Premium Trial Feature App – Not for Distribution"** filigranı gösterir. Mağazada yayın için ücretli plan gerekir: https://developer.vuforia.com/pricing
- Lisans anahtarını herkese açık depoya koymayın. Bizde `Assets/Resources/VuforiaConfiguration.asset` `.gitignore` ile dışlanır.

---

## 2. Canlı uygulama (27 dk)

### Adım 0 — Hazırlık (derse gelmeden)

- Unity 6000.3.27f1 + Android Build Support.
- developer.vuforia.com hesabı, **Basic lisans anahtarı** (Plan & Licenses › Generate Basic License).
- Vuforia Engine 11.4.4 paketi `Packages/` klasörüne.
- Android telefon (Android 10+), USB hata ayıklama açık.

### Adım 1 — Simülasyonda göster (3 dk)

`Assets/G6/Scenes/G6_NesneTakibi.unity` › Play. Kart iki saniye sonra bulunur. Sırayla gösterin: Akım, Pinler, Blink, Patlat, Quiz. `F` ile takip kaybını gösterin.

### Adım 2 — Modeli koddan üret (3 dk)

`Assets/G6/Editor/ArduinoMegaModel.cs` dosyasını açın. Bir parçanın PCB koordinatını (mm) gösterin. Menü: **G6 › Sahneyi Kur** ile ikizi yeniden kurun.

### Adım 3 — Model Target üret (5 dk)

1. Menü: **G6 › Vuforia › Model Target dosyalarını hazırla (DAE + JSON)**. `ModelTarget/arduino_mega.dae` ve `ModelTarget/model_target.json` oluşur.
2. JSON'daki `views` listesinde kılavuz açılarını gösterin.
3. `Tools/ModelTargetUret.ps1` betiğini çalıştırın. Vuforia e-posta ve şifresini betik kendi penceresinde sorar.
4. `ModelTarget/cikti/` içindeki veritabanını Unity'ye aktarın.

### Adım 4 — Image Target fotoğrafı (4 dk)

1. Kartı beyaz bir kâğıda koyun, USB solda olsun. Tam üstten, flaşsız fotoğraf çekin.
2. Fotoğrafı kart kenarlarından kırpıp düzeltin (biz OpenCV `warpPerspective` kullandık).
3. `Assets/StreamingAssets/Vuforia/arduino_ust.jpg` olarak kaydedin.

### Adım 5 — Sahneyi bağla ve derle (6 dk)

1. Menü: **G6 › Vuforia › AR sahnesini kur**. Kamera Vuforia kamerasına dönüşür; hedefleri çalışma anında oluşturan `VuforiaModelTargetLoader` eklenir.
2. **G6 › Derle › Android APK**.

### Adım 6 — Cihazda dene (4 dk)

Kartı masaya koyun, telefonu 15–30 cm uzaktan tutun. Hologram kartın üstüne oturur. Kartı kadrajdan çıkarıp geri getirin.

### Adım 7 — Sınıf atölyesi (kalan süre)

Her öğrenci kendi nesnesiyle (ör. hesap makinesi, Arduino Uno, kumanda):
1. `ArduinoMegaModel.cs` dosyasını örnek alıp yeni bir profil tanımlar.
2. Simülasyonda etiketleri dener.
3. Bir parçaya kendi "Biliyor muydun?" bilgisini yazar.

---

## 3. Yapay zekâ ile çalışma (anlatımda gösterilecek)

İstemler ve yapay zekânın hataları: `Docs/Istem_Gunlugu.md`. Canlı gösterim önerisi: Hata 5 (Android'de collider sınıfının silinmesi) ya da Hata 7 (zımbanın tam oturmaması ve nesne değiştirme kararı).

## 4. Sık yapılan hatalar

| Belirti | Neden | Çözüm |
|---|---|---|
| Takip hiç başlamıyor | Model ölçüleri gerçek nesneden farklı veya birim yanlış (mm/m) | Ölçüleri kontrol edin, birim metre olmalı |
| Yalnızca tek açıdan tanıyor | Model Target yalnızca etkin kılavuz açısına yakın bakışta tanır | Birden çok kılavuz açısı tanımlayın veya Image Target ekleyin |
| Hologram nesneden kaymış | Model, hedef üretildikten sonra değişti | Modeli ve Model Target'ı yeniden üretin |
| Poz takla atıyor | Nesne simetrik | Asimetrik nesne seçin |
| Android'de "SphereCollider doesn't exist" | IL2CPP kod ayıklaması, yalnızca koddan eklenen sınıfı sildi | `link.xml` ile sınıfı koruyun |
| Derleme: minimum API hatası | Vuforia 11 Android API 29 ister | Player Settings › Minimum API Level 29 |
| OBJ'de sayılar virgüllü | Türkçe bölge ayarı ondalık virgül kullanır | `CultureInfo.InvariantCulture` kullanın |
| Siyah kamera ekranı | Kamera izni reddedildi | Ayarlar › Uygulamalar › İzinler › Kamera |
