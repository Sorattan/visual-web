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
<img width="4035" height="3255" alt="Mimari" src="https://github.com/user-attachments/assets/de2ef1cf-5e48-4a73-9be5-c07430876d76" />

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

## Ekran Görüntüleri
- Graf çizimi + node seçimi
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 215306" src="https://github.com/user-attachments/assets/5ba4e3e4-2858-4166-948b-00a15a62b376" />
<br>
<br>

- Node sürükleme ve Ctrl+Scroll zoom
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 215337" src="https://github.com/user-attachments/assets/df3d6299-9720-4098-a482-d4d9f79884d1" />
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 215401" src="https://github.com/user-attachments/assets/f0c7d257-4360-4983-a787-352609e7b4d6" />
<br>
<br>

- CSV/JSON import/export
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 215423" src="https://github.com/user-attachments/assets/0f70f609-6000-417e-a739-dd55f893bd46" />
<img width="441" height="1009" alt="Ekran görüntüsü 2026-01-01 212817" src="https://github.com/user-attachments/assets/ec905ff4-be29-4077-bd6f-cc7e1c97dc12" />
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 215447" src="https://github.com/user-attachments/assets/a57b70c2-f12d-4c64-88c4-2a3714a07207" />
<br>
<br>

- Komşuluk listesi ve matrisi çıktısı
<img width="319" height="987" alt="Ekran görüntüsü 2026-01-01 213036" src="https://github.com/user-attachments/assets/45f8c721-2cc9-4704-bc52-15b0e30644db" />
<img width="1159" height="1002" alt="Ekran görüntüsü 2026-01-01 213112" src="https://github.com/user-attachments/assets/226a7ad0-7e6f-40b0-9edb-261c708ecc32" />
<br>
<br>

- BFS/DFS sonucu highlight
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 220701" src="https://github.com/user-attachments/assets/69d5b8d3-217c-4056-b552-dc534c6a6435" />
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 220627" src="https://github.com/user-attachments/assets/9f8a5750-47de-46fb-8ed8-56cb8a518c41" />
<br>
<br>

- Connected Components renklendirme
<img width="2560" height="1440" alt="image" src="https://github.com/user-attachments/assets/28dd4754-0951-4d3e-91e9-6ecf180ff8bc" />
<br>
<br>

- Welsh–Powell renklendirme + sonuç listesi
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 215538" src="https://github.com/user-attachments/assets/e3fd36c8-8c57-4228-b93b-6f7039e8b10e" />
<br>
<br>

- Dijkstra ve A* en kısa yol (edge’ler kalın/altın)
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 221019" src="https://github.com/user-attachments/assets/e11a15dd-c299-41a2-89e1-f44fd5505e4a" />
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 221023" src="https://github.com/user-attachments/assets/9f4ac54f-85aa-498e-93fe-a18678f28875" />
<br>
<br>

- Benchmark sonuç DataGrid’i
<img width="2560" height="1440" alt="Ekran görüntüsü 2026-01-01 215733" src="https://github.com/user-attachments/assets/7bd684d9-ab76-42ab-a8de-40d4ea5dd88c" />
<img width="330" height="237" alt="image" src="https://github.com/user-attachments/assets/02da471c-7dd4-41f0-9e07-68a6ff8888dd" />
