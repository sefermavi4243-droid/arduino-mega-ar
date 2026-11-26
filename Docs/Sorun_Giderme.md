# Sorun Giderme

Kurulum ve sahada deneme sırasında en sık karşılaşılan sorunlar ve çözümleri.

| Belirti | Olası neden | Çözüm |
|---|---|---|
| Unity açılışta `com.ptc.vuforia.engine` bulunamadı hatası veriyor | Vuforia paketi `Packages/` içinde yok | `com.ptc.vuforia.engine-11.4.4.tgz` dosyasını `Packages/` klasörüne koyup projeyi yeniden açın. |
| Kamera açılıyor ama hiçbir şey algılanmıyor | Lisans anahtarı girilmemiş | **Window › Vuforia Configuration › App License Key** alanını doldurun. |
| Kart bulunuyor, model titriyor | Ortam ışığı yetersiz ya da kart parlıyor | Dağınık ışık kullanın, kartı doğrudan lamba altından uzaklaştırın. |
| Kart hiç bulunmuyor | Farklı bir klon kart kullanılıyor | Kendi kartınızın üstten fotoğrafını `Assets/StreamingAssets/Vuforia/arduino_ust.jpg` olarak koyun. |
| Ekranda "Premium Trial Feature" filigranı | Ücretsiz lisansla Model Target | Beklenen davranış. Dağıtım için ücretli lisans gerekir. |
| Komut satırı derlemesi lisans hatası (198) veriyor | Unity Hub kapalı | Unity Hub'ı açık bırakıp `-batchmode` komutunu yeniden çalıştırın. |
| Git Bash'te `adb push ... /sdcard/...` yolu bozuluyor | MSYS yol dönüştürmesi | Komutun başına `MSYS_NO_PATHCONV=1` ekleyin. |
| Sahne bozuldu ya da nesneler kayboldu | Elle yapılan değişiklikler | **G6 › Sahneyi Kur** menüsüyle sahneyi sıfırdan kurun. |
