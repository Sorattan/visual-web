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
<img  style="max-width: 500px; width: 60%; height: auto;" alt="Mimari" src="https://github.com/user-attachments/assets/c8de6563-ed11-4ada-b689-7f2caffebe02" />

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
- #### BFS
<table>
  <tr>
    <td width="65%" valign="top">
      <h3>BFS (Breadth-First Search)</h3>
      <p><b>Amaç:</b> Başlangıç düğümünden itibaren düğümleri katman katman dolaşır.</p>
      <p><b>Çalışma mantığı:</b> FIFO kuyruk (Queue) kullanır. Ziyaret edilmemiş komşular kuyruğa eklenir.</p>
      <p><b>Karmaşıklık:</b> O(V + E)</p>
      <p><b>Çıktı:</b> Ziyaret sırası (visited order)</p>
    </td>
    <td width="35%" valign="top" align="center">
      <img src="https://github.com/user-attachments/assets/39367c97-6551-4f64-a0a4-3c3036614853" width="320" alt="BFS">  
    </td>
  </tr>
</table>

- #### DFS
<table>
  <tr>
    <td width="65%" valign="top">
      <h3>DFS (Depth-First Search)</h3>
      <p><b>Amaç:</b> Grafı derinlemesine gezerek düğümleri keşfeder; bileşen bulma ve birçok graf analizinin temelini oluşturur.</p>
      <p><b>Çalışma mantığı:</b> LIFO yığın (Stack) ya da recursion kullanır. <i>currentId</i> alınır, ziyaret edilmemişse işaretlenir ve komşuları stack’e eklenir.</p>
      <p><b>Karmaşıklık:</b> O(V + E)</p>
      <p><b>Çıktı:</b> Ziyaret sırası (visited order) ve keşif yapısı.</p>
    </td>
    <td width="35%" valign="top" align="center">
      <img src="https://github.com/user-attachments/assets/d0839378-8de1-4f9b-a33d-18cceda56f44" width="320" alt="DFS">
    </td>
  </tr>
</table>

- #### Connected Components
<table>
  <tr>
    <td width="65%" valign="top">
      <h3>Connected Components (Bağlı Bileşenler)</h3>
      <p><b>Amaç:</b> Grafı, birbirine erişebilen düğüm kümelerine (bileşenlere) ayırır.</p>
      <p><b>Çalışma mantığı:</b> Tüm düğümler taranır. Ziyaret edilmemiş bir düğüm görüldüğünde BFS/DFS başlatılır; bulunan düğümler bir bileşeni oluşturur.</p>
      <p><b>Karmaşıklık:</b> O(V + E)</p>
      <p><b>Çıktı:</b> Bileşen listesi (örn. Bileşen 1: {…}, Bileşen 2: {…}).</p>
    </td>
    <td width="35%" valign="top" align="center">
      <img src="https://github.com/user-attachments/assets/9c787fa1-f45f-443e-9ce3-a68e88643e85" width="320" alt="Connected_Components">
    </td>
  </tr>
</table>

- #### Degree Centrality
<table>
  <tr>
    <td width="65%" valign="top">
      <h3>Degree Centrality (Derece Merkeziliği)</h3>
      <p><b>Amaç:</b> Her düğümün “ne kadar bağlantılı” olduğunu ölçer. Sosyal ağlarda hızlı ve temel bir merkezilik metriğidir.</p>
      <p><b>Çalışma mantığı:</b> Her <i>nodeId</i> için <i>degree(nodeId)</i> hesaplanır. İstenirse normalize edilir: <i>degree/(N−1)</i>. Sonuçlar sıralanıp Top-k gösterilir.</p>
      <p><b>Karmaşıklık:</b> Hesaplama O(V + E), sıralama ile toplam O(E + V log V)</p>
      <p><b>Çıktı:</b> Node → (degree, score) listesi, Top-5 tablo/etiketleme.</p>
    </td>
    <td width="35%" valign="top" align="center">
      <img src="https://github.com/user-attachments/assets/c4dfe875-1c13-4644-bbec-f550271c0d54" width="320" alt="Degree_Centrality">
    </td>
  </tr>
</table>

- #### Welsh–Powell Graph Coloring
<table>
  <tr>
    <td width="65%" valign="top">
      <h3>Welsh–Powell Graph Coloring (Graf Boyama)</h3>
      <p><b>Amaç:</b> Komşu düğümler aynı rengi almayacak şekilde düğümleri renklendirir. (Greedy/heuristic; minimum renk garantisi yoktur.)</p>
      <p><b>Çalışma mantığı:</b> Düğümler dereceye göre azalan sıralanır. Her <i>currentId</i> için komşu renkleri toplanır ve kullanılmayan en küçük renk atanır: <i>colorOf[currentId]</i>.</p>
      <p><b>Karmaşıklık:</b> Sıralama O(V log V); komşu kontrolüne bağlı olarak pratikte O(E) civarı, toplamda genelde O(E + V log V).</p>
      <p><b>Çıktı:</b> Node → Color tablosu ve graf üzerinde renklendirme.</p>
    </td>
    <td width="35%" valign="top" align="center">
      <img src="https://github.com/user-attachments/assets/2d0787cb-7d05-4083-ae1a-48bc6eccf4fd" width="320" alt="WelshPowell">
    </td>
  </tr>
</table>

- #### Dijkstra
<table>
  <tr>
    <td width="65%" valign="top">
      <h3>Dijkstra (En Kısa Yol)</h3>
      <p><b>Amaç:</b> Negatif olmayan ağırlıklarda start → target en kısa yolu bulur.</p>
      <p><b>Çalışma mantığı:</b> <i>dist[startId]=0</i>, diğerleri ∞. Priority Queue (PQ) ile en küçük <i>dist</i>’e sahip <i>currentId</i> seçilir. Her komşu için <i>alternative = dist[current] +     weight(current, neighbor)</i> hesaplanır; daha iyiyse <i>dist</i> ve <i>prev</i> güncellenir (relax).</p>
      <p><b>Karmaşıklık:</b> PQ ile O((V + E) log V)</p>
      <p><b>Çıktı:</b> En kısa yol (node dizisi) + toplam maliyet, graf üzerinde yol vurgusu.</p>
    </td>
    <td width="35%" valign="top" align="center">
      <img src="https://github.com/user-attachments/assets/07dfec6a-e536-454f-bc52-e687f5d7eb0f" width="320" alt="Dijkstra">
    </td>
  </tr>
</table>

- #### A*
<table>
  <tr>
    <td width="65%" valign="top">
      <h3>A* (A-Star) (Sezgisel En Kısa Yol)</h3>
      <p><b>Amaç:</b> Dijkstra’nın garantisini koruyup (uygun heuristic ile) hedefe daha hızlı yönelerek aramayı pratikte hızlandırmak.</p>
      <p><b>Çalışma mantığı:</b> <i>gScore</i> gerçek maliyet, <i>hScore=heuristic(node)</i> hedefe tahmin, <i>fScore=gScore+hScore</i>. PQ, en küçük <i>fScore</i>’u seçer. Komşular için daha iyi <i>gScore</i> bulunursa <i>cameFrom</i>, <i>gScore</i>, <i>fScore</i> güncellenir.</p>
      <p><b>Karmaşıklık:</b> Worst-case O((V + E) log V) (Dijkstra’ya yaklaşır), iyi heuristic ile pratikte daha hızlı olabilir.</p>
      <p><b>Çıktı:</b> En kısa yol + maliyet, graf üzerinde yol vurgusu.</p>
    </td>
    <td width="35%" valign="top" align="center">
      <img src="https://github.com/user-attachments/assets/3f4491fd-2cf7-4f69-862e-2a520c542395" width="320" alt="A">
    </td>
  </tr>
</table>

### Performans Testleri
- N node / E edge / seed ile rastgele graf üret
- Tüm algoritmaların sürelerini ölç (µs, ms) ve DataGrid’de göster
- Benchmark CSV kaydet

## Kurulum & Çalıştırma

1. Visual Studio ile `.sln` dosyasını aç.
2. `SocialNetworkAnalyzer.App` projesini **Startup Project** yap.
3. `F5` ile çalıştır.

### Kolay Çalıştırma 
1. [exe.zip](exe.zip?raw=true) dosyasını indir, ayıkla.
2. `SocialNetworkAnalyzer.App.exe` dosyasını çalıştır.

## CSV Formatı

Çıktı Formatı:
DugumId Ozellik_I Ozellik_II Ozellik_III Komsular

- `Ozellik_I`: Activity
- `Ozellik_II`: Interaction
- `Ozellik_III`: Bağlantı sayısı (degree)
- `Komsular`: Virgülle ayrılmış komşu id’leri

Çıktı Örneği:
1 0.8 12 3 2,4,5

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
