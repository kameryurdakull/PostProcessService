# Post Process Demo

`Assets/Scenes/PostProcessDemo.unity` sahnesini açıp Play'e basın. Sol paneldeki 12 buton seçilen Volume görünümünü uygular; aynı butona tekrar basmak görünümü kapatır. **RESET / BASELINE** tüm aktif efektleri ve kamera hareketini sıfırlar. Motion Blur butonu, etkinin görülebilmesi için kamerayı yatay hareket ettirir.

Her buton `Profiles` klasöründeki bir `PostProcessPreset` asset'ine bağlıdır. Preset, ayrı bir URP `VolumeProfile` taşır. Renk, mod ve LUT gibi sayısal olmayan alanlar bu profil üzerinden düzenlenir. `TealAmberLut.png`, projenin 32 boyutlu URP LUT ayarına uygun 1024×32 dokudur.

`PostProcessVolumeExtension`, sayısal alanlar için `Set`, `Pulse` ve `Reset`; profil geçişleri için `PlayPreset`, `StopPreset` ve `ResetPresets`; temel Volume için `BlendWeight` sunar. Preset kanalları **Environment**, **Gameplay** ve **Cinematic** sırasıyla öncelik alır. Aynı kanaldaki yeni preset, eskisiyle geçişli olarak yer değiştirir.

Demo sahnesi `Tools > Post Processing > Build Demo Scene` menüsünden tekrar üretilebilir. Bu işlem sahneyi baştan oluşturur; mevcut Volume Profile asset'lerindeki elle yapılmış efekt ayarlarını korur.
