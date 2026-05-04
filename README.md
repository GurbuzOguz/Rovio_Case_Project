# Rovio Case Project

Unity ile geliştirilen, renk bazli kutularla grid uzerindeki urunleri toplama odakli bir puzzle/prototip oyun projesi.

## Proje Ozeti

- Ana sahne: `Assets/Scenes/Game.unity`
- Oyun dongusu: kutu sec -> rota uzerinde hareket -> uygun renkte urunleri topla -> grid kaydirma uygula -> tum urunler bitince level tamamla
- Level akisi `PlayerPrefs` uzerinden tutulur ve level tamamlandiginda bir sonraki levele gecilir
- Mimari Zenject tabanli servis baglamalari ile kuruludur

## Teknik Bilgiler

- Unity Editor surumu: `6000.3.2f1`
- Render Pipeline: URP (`com.unity.render-pipelines.universal`)
- Input: Unity Input System (`com.unity.inputsystem`)
- DI: Zenject
- Tween/animasyon yardimcilari: DOTween
- UI: TextMeshPro + UGUI

## Nasil Calistirilir

1. Unity Hub uzerinden `Rovio_Case` klasorunu acin.
2. Unity Editor olarak `6000.3.2f1` (veya uyumlu bir 6000.3.x) surumunu secin.
3. `Assets/Scenes/Game.unity` sahnesini acin.
4. Play tusuna basin.

## Oynanis Kurali (Kisa)

- Kutular tiklanarak aktive edilir.
- Kutu, path uzerinde ilerlerken kendi rengine uygun urunleri toplar.
- Toplanan hucreden sonra ilgili satir/sutunda kaydirma uygulanir.
- Kutu kapasitesi dolarsa kutu devre disi kalir.
- Bench dolulugu gibi durumlar level fail durumuna gidebilir.
- Gridde urun kalmadiginda level complete durumu tetiklenir.

## Proje Yapisi

- `Rovio_Case/Assets/Scripts/Game`: state ve level flow servisleri
- `Rovio_Case/Assets/Scripts/Boxes`: box spawn, queue, movement, collect ve bench modulleri
- `Rovio_Case/Assets/Scripts/Services`: grid data/shift mantigi
- `Rovio_Case/Assets/Scripts/Products`: urun etkileşim ve gorsel akis
- `Rovio_Case/Assets/Scripts/UI`: HUD ve level end ekrani yonetimi
- `Rovio_Case/Assets/Scripts/Installers`: Zenject binding kurulumlari
- `Rovio_Case/Assets/Scripts/Editor`: level uretim ve hizli test editor araclari

## Editor Araclari

- `Tools/Level/Pixel Level Editor`:
  - texture'dan `LevelLayout` urun hucrelerini ve palette olusturma
- `Tools/Level/Level Quick Start`:
  - level index secip hizli baslatma / test

## Gelistirme Notlari

- Build ayarlarinda aktif oyun sahnesi: `Assets/Scenes/Game.unity`
- Level siralamasi `LevelSequenceConfig` ile yonetilir
- Aktif level index anahtari: `LevelPrefsKeys.CurrentLevelIndex`

## Lisans ve Ucuncu Parti

Projede cesitli ucuncu parti Unity paketleri/assetleri kullanilmistir (Zenject, DOTween, Feel vb.). Lisans detaylari ilgili paket klasorlerindeki dokumanlarda bulunur.
