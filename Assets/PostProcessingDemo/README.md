# Post Process Demo

`Assets/Scenes/PostProcessDemo.unity` sahnesini açıp Play'e basın. Sol paneldeki 12 butondan birden fazlasını seçerek efektleri aynı anda karıştırabilirsiniz. Seçili butona tekrar basmak yalnızca o efekti kapatır. Sağ üstteki **MIX** kartı aktif efekt sayısını ve adlarını gösterir. **RESET / BASELINE** tüm aktif efektleri ve kamera hareketini sıfırlar. Motion Blur seçili kaldığı sürece kamera yatay hareket eder.

Her buton `Profiles` klasöründeki bir `PostProcessPreset` asset'ine bağlıdır. Preset, ayrı bir URP `VolumeProfile` taşır. Renk, mod ve LUT gibi sayısal olmayan alanlar bu profil üzerinden düzenlenir. `TealAmberLut.png`, projenin 32 boyutlu URP LUT ayarına uygun 1024×32 dokudur.

`PostProcessVolumeExtension`, sayısal alanlar için `Set`, `Pulse` ve `Reset`; profil geçişleri için `PlayPreset`, `StopPreset` ve `ResetPresets`; temel Volume için `BlendWeight` sunar. Preset kanalları **Environment**, **Gameplay** ve **Cinematic** sırasıyla öncelik alır. `Stackable` işaretli preset'ler aynı kanalda birlikte çalışır; aynı Volume alanını değiştirenlerde en son açılan preset öncelik kazanır. `Stackable` işaretsiz preset açıldığında aynı kanaldaki aktif preset'ler geçişli olarak kapanır.

Demo sahnesi `Tools > Post Processing > Build Demo Scene` menüsünden tekrar üretilebilir. Bu işlem sahneyi baştan oluşturur; mevcut Volume Profile asset'lerindeki elle yapılmış efekt ayarlarını korur.
