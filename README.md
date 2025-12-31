# SocialNetworkAnalyzer (WPF)

- Ömer Faruk Sarı
- Emirhan Bıkmaz

WPF tabanlı bir sosyal ağ analiz uygulaması. Kullanıcı arayüzünde graf görselleştirilir; node/edge CRUD yapılır; CSV/JSON içe-dışa aktarma, komşuluk listesi/matrisi üretimi ve temel graf algoritmaları (BFS/DFS, Connected Components, Degree Centrality, Welsh–Powell, Dijkstra, A*) çalıştırılır. Ayrıca performans benchmark ekranı ile süre ölçümleri alınır.

## Proje Yapısı

- **SocialNetworkAnalyzer.Core**
  - `Models/` (Node, Edge, Graph)
  - `Validation/` (GraphValidationException vb.)
  - `Weights/` (DynamicWeightCalculator, IWeightCalculator)
  - `Algorithms/` (Traversals, ConnectedComponents, Centrality, GraphColoring, ShortestPaths)
  - `IO/` (GraphIO: CSV/JSON import-export, adjacency list/matrix)
- **SocialNetworkAnalyzer.App**
  - WPF UI (`MainWindow.xaml`, `MainWindow.xaml.cs`)
- **SocialNetworkAnalyzer.Tests**
  - Unit testler

## Özellikler

### Görselleştirme & Etkileşim
- Canvas üzerinde graf çizimi
- Node seçme → sağ panelde node detayları
- Node sürükle-bırak (basılı tutup taşıma)
- **Ctrl + MouseWheel** ile zoom

### CRUD
- Node ekle / güncelle / sil
- Edge ekle / sil

### Veri İçe / Dışa Aktarma
- **CSV Export/Import**
- **JSON Export/Import** (X,Y dahil olduğu için görsel yerleşimi korur)
- Komşuluk listesi üretimi (txt)
- Komşuluk matrisi üretimi (csv)

### Algoritmalar
- BFS, DFS (ziyaret sırası + highlight)
- Connected Components (bileşenleri bul + farklı renge boya)
- Degree Centrality (Top-5 liste + highlight)
- Welsh–Powell Graph Coloring (node→color tablosu + renklendirme)
- Dijkstra (dinamik ağırlıklarla en kısa yol)
- A* (heuristic: koordinat tabanlı)

### Performans Testleri
- N node / E edge / seed ile rastgele graf üret
- Tüm algoritmaların sürelerini ölç (µs, ms) ve DataGrid’de göster
- Benchmark CSV kaydet

## Kurulum & Çalıştırma

1. Visual Studio ile `.sln` dosyasını aç.
2. `SocialNetworkAnalyzer.App` projesini **Startup Project** yap.
3. `F5` ile çalıştır.

## CSV Formatı

Başlık örneği:
DugumId Ozellik_I Ozellik_II Ozellik_III Komsular

Satır örneği:
1 0.8 12 3 2,4,5

- `Ozellik_I`: Activity
- `Ozellik_II`: Interaction
- `Ozellik_III`: Bağlantı sayısı (degree)
- `Komsular`: Virgülle ayrılmış komşu id’leri

## JSON Formatı

JSON export; node özellikleri + X,Y (görsel konum) + edges içerir.

## Kullanım

1) Node/Edge ekleyerek graf oluştur  
2) CSV/JSON ile içe aktar/dışa aktar  
3) Algoritmalar bölümünden BFS/DFS/Dijkstra/A* vb. çalıştır  
4) Performans Testleri bölümünden graf üret + benchmark çalıştır

## Ekran Görüntüleri (Rapor için öneri)
- Graf çizimi + node seçimi
- Node sürükleme ve Ctrl+Scroll zoom
- CSV/JSON import/export
- Komşuluk listesi ve matrisi çıktısı
- BFS/DFS sonucu highlight
- Connected Components renklendirme
- Welsh–Powell renklendirme + sonuç listesi
- Dijkstra ve A* en kısa yol (edge’ler kalın/altın)
- Benchmark sonuç DataGrid’i

## Test

Test Explorer → Run All  
veya `SocialNetworkAnalyzer.Tests` üzerinden unit test çalıştır.

