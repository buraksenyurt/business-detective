# Business Detective

Modernize edilmek istenen bir uygulamadaki business nesnelerin tamamının birer sözleşmesini çıkarmak istediğimizi düşünelim. Her business nesne için birer interface oluşturacağız. Hatta uygulamanın .Net Framework tabanlı olduğunu ve bu sözleşmeler için birer WCF servis noktası üretmek istediğimizi farz edelim. Biraz rosyln'den yararlanarak işi otomatize edebilir miyiz, edebiliyorsak ne kadar ileri gidebiliriz sorularına cevap bulmaya çalışıyoruz.

Örnek Solution'ın ilk versiyonunda aşağıdaki klasör yapısının söz konusu olduğunu düşünelim _(Bu oldukça ufaltılmış bir solution)_

+ BusinessLibrary
+ BusinessContracts
+ Entity

___Sınıfları elle interface haline getirmek veya dönüştürme işini üstlenen BusinessConverter'dan diğer projeleri referans ederek çalışmak yasak!___

İstenenler şunlar:

+ BusinessLibrary içerisinde olup BCCommon'dan türeyen sınıflar için birer Interface nesnesi oluşturulsun ve BusinessContract altına aynı klasör hiyerarşisine göre eklensinler.
+ Interface'i çıkarılan sınıflara interface implementasyon bildirimleri eklensin.
+ Interface'ler ServiceContract ve OperationContract nitelikleri ile donatılsın.
+ Her bir servis sözleşmesi için standart bir config dosyasında kullanılabilmesi için otomatik endPoint tanımları üretilsin.

## Çözüm Teorisi

Solution kod tarafında açılı ve BusinessLibrary içerisindeki BCCommon türevli sınıflar keşfedilerek çeşitli kurallara göre interface tiplerinin oluşturulması sağlanır. Ayrıca o anki sınıfın bu interface tipini kullanması için gerekli ilaveler yapılır. İşlemler sırasında bir listede toplanan interface tipleri BusinessContracts içerisinde de eklenir. Interface tipleri oluşturulurken WCF için gerekli niteliklerle donatılır ve uygun config endPoint'lerinin çıkarılması sağlanır. Tabii çözüm sırasında kodun ağaç yapısından _(syntax tree)_ yararlanmak iyi bir fikir olabilir.

![./assets/screenshot_2.png](./assets/screenshot_2.png)

## Denemeler

Denemek için BusinessConverter console uygulamasının çalıştırılması yeterlidir. Aşağıdakine benzer bir çıktı oluşması gerekir. Ben sonuçları __process__ isimli branch'te topluyorum.

![./assets/screenshot_1.png](./assets/screenshot_1.png)

## Daha Neler Yapılabilir?

+ BusinessHost isimli bir uygulama yazılıp WCF servis endpoint'lerinin işler olması sağlanabilir.
+ Araç komut satırından parametre alarak herhangi bir çözüme uygulanabilir hale getirilebilir.
+ Çözüm belli bir uygulamanın desenine özgü oldu. Generic yapılabilir.
