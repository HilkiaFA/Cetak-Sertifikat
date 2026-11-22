# Panduan Lengkap Aplikasi Cetak Sertifikat Massal

Aplikasi desktop Windows modern berbasis C# WPF (.NET 10) untuk mendesain template dan mencetak serta mengekspor sertifikat secara massal dengan data dinamis dari Excel, CSV, Google Sheets, atau input manual.

---

## Daftar Isi

1. Kebutuhan Sistem
2. Cara Mendownload dan Mendapatkan Aplikasi
3. Cara Menjalankan Aplikasi
4. Alur Kerja dan Cara Penggunaan
   - Tahap 1: Menentukan Ukuran Kertas dan Upload Background
   - Tahap 2: Menyiapkan dan Mengimpor Data Penerima
   - Tahap 3: Mengatur Elemen Teks di Atas Kanvas
   - Tahap 4: Navigasi dan Pratinjau Data Real-time
   - Tahap 5: Ekspor Massal dan Cetak Langsung
   - Tahap 6: Menyimpan dan Membuka Template Desain
5. Daftar Pintasan Keyboard (Shortcuts)
6. Solusi Masalah Umum (Troubleshooting)
7. Struktur Proyek dan Teknologi

---

## 1. Kebutuhan Sistem

Sebelum menjalankan aplikasi, pastikan perangkat komputer memenuhi persyaratan berikut:

- Sistem Operasi: Windows 10 (versi 1809 ke atas) atau Windows 11 (arsitektur 64-bit / x64).
- Runtime Framework: .NET Desktop Runtime 10.0 (Windows Desktop Runtime x64).
  - Jika belum terpasang, unduh dari situs resmi Microsoft .NET (pilih "Run desktop apps" untuk .NET 10).
- Memori (RAM): Minimal 4 GB (disarankan 8 GB untuk pemrosesan ratusan sertifikat resolusi tinggi).
- Resolusi Layar: Minimal 1280 x 720 piksel (disarankan 1920 x 1080 piksel).

---

## 2. Cara Mendownload dan Mendapatkan Aplikasi

### Opsi A: Menggunakan File Siap Pakai (Executable)

Jika Anda mendapatkan paket rilis aplikasi atau mengunduh folder proyek:

1. Buka folder hasil build atau publikasi pada direktori berikut:
   - Folder Publish: `bin\Publish\`
   - Folder Release: `bin\Release\net10.0-windows\`
2. Pada folder tersebut, berkas utama aplikasi bernama:
   `CetakSertifikatApp.exe`
3. Pastikan seluruh file `.dll` pendukung (seperti SkiaSharp, ClosedXML, CsvHelper) dan folder `runtimes` tetap berada di samping file `CetakSertifikatApp.exe`. Jangan memindahkan file `.exe` sendirian ke luar folder tanpa berkas pendukungnya.

### Opsi B: Mengunduh Source Code dari Git

Jika Anda mengunduh dari repository kode sumber:

1. Buka terminal (Command Prompt atau PowerShell).
2. Jalankan perintah git clone:
   ```bash
   git clone <URL_REPOSITORY>
   cd CetakSertifikat
   ```
3. Lakukan build aplikasi menggunakan .NET SDK (dijelaskan pada bagian berikutnya).

---

## 3. Cara Menjalankan Aplikasi

### Cara 1: Menjalankan Langsung via File .EXE (Pengguna Umum)

1. Buka Windows Explorer dan arahkan ke folder:
   `c:\Users\asust\source\repos\CetakSertifikat\bin\Publish\`
   atau
   `c:\Users\asust\source\repos\CetakSertifikat\bin\Release\net10.0-windows\`
2. Klik ganda pada file `CetakSertifikatApp.exe`.
3. Aplikasi akan langsung terbuka dan menampilkan template contoh bawaan lengkap dengan gambar background 300 DPI dan kotak teks contoh.

### Cara 2: Menjalankan via Terminal / Perintah .NET (Pengembang)

1. Buka PowerShell atau Command Prompt.
2. Arahkan ke folder proyek:
   ```powershell
   cd c:\Users\asust\source\repos\CetakSertifikat
   ```
3. Jalankan perintah berikut:
   ```powershell
   dotnet run
   ```

### Cara 3: Melakukan Build Ulang Sendiri (Compile dari Source Code)

Jika Anda mengubah kode atau ingin menghasilkan executable baru:

1. Build mode Release:
   ```powershell
   dotnet build CetakSertifikatApp.csproj -c Release
   ```
2. Publikasikan seluruh aset ke dalam satu folder siap pakai:
   ```powershell
   dotnet publish CetakSertifikatApp.csproj -c Release -o bin\Publish
   ```
3. Menjalankan pengujian otomatis (Unit Tests):
   ```powershell
   dotnet test Tests/CetakSertifikatApp.Tests.csproj
   ```

---

## 4. Alur Kerja dan Cara Penggunaan

### Tahap 1: Menentukan Ukuran Kertas dan Upload Background

1. **Pilih Ukuran Kertas**:
   - Di panel kiri atas, terdapat menu pilihan ukuran kertas:
     - A4 Lanskap (297 x 210 mm) - default sertifikat umum
     - A4 Potret (210 x 297 mm)
     - Letter Lanskap (279.4 x 215.9 mm)
     - Letter Potret (215.9 x 279.4 mm)
     - Ukuran Kustom (dapat memasukkan lebar dan tinggi dalam milimeter secara bebas)
2. **Upload Gambar Background**:
   - Klik tombol "Upload Background" pada toolbar atas.
   - Pilih file gambar desain sertifikat Anda (format PNG, JPG, atau JPEG).
   - Sistem akan otomatis mendeteksi resolusi gambar. Jika resolusi gambar di bawah 150 DPI untuk ukuran kertas yang dipilih, sistem akan menampilkan peringatan bahwa hasil cetak berpotensi buram, dan menyarankan gambar beresolusi 300 DPI.
3. **Template Contoh Bawaan**:
   - Saat aplikasi pertama dibuka, sistem telah memuat background contoh beresolusi 300 DPI dengan layout teks lengkap sehingga Anda dapat langsung mencoba seluruh fitur tanpa perlu menyiapkan gambar terlebih dahulu.

---

### Tahap 2: Menyiapkan dan Mengimpor Data Penerima

Aplikasi menyediakan berbagai cara mudah untuk memasukkan data penerima sertifikat:

#### Metode A: Mengunduh Template Excel Resmi Bawaan Aplikasi
1. Klik tombol "Unduh Template Excel" di toolbar atas atau di dalam dialog data.
2. Simpan file `Template_Data_Sertifikat.xlsx`.
3. Keunggulan file template ini:
   - Kolom-kolomnya otomatis menyesuaikan dengan variabel teks yang ada pada desain sertifikat Anda saat itu (misalnya: `{nama}`, `{nomor}`, `{peran}`, `{instansi}`, `{tanggal}`).
   - Terdiri dari 2 sheet:
     - Sheet 1 ("Data Penerima"): Berisi tabel berformat rapi siap diisi data penerima.
     - Sheet 2 ("Panduan Pengisian"): Berisi tips penulisan gelar, tata cara copy-paste, dan panduan Google Forms.
4. Isi data peserta pada file Excel tersebut, lalu simpan.
5. Klik "Kelola Data Penerima" -> "Buka File Excel (.xlsx)", pilih file tersebut.

#### Metode B: Integrasi dari Google Forms dan Google Sheets
1. Buat form registrasi/absen di Google Forms dengan nama pertanyaan yang sesuai, misalnya:
   - "Nama Lengkap dan Gelar" (akan menjadi `{nama}`)
   - "Instansi / Lembaga" (akan menjadi `{instansi}`)
   - "Peran" (akan menjadi `{peran}`)
2. Hubungkan form ke Google Sheets melalui tab Responses -> "Link to Sheets".
3. Ada dua cara memindahkan data dari Google Sheets ke aplikasi:
   - Cara Cepat (Copy Paste): Blok seluruh tabel Google Sheets dari baris judul sampai baris terakhir data, tekan Ctrl+C. Buka "Kelola Data Penerima" di aplikasi, lalu klik "Tempel dari Clipboard (Ctrl+V)". Data langsung terisi seketika.
   - Cara Download: Di Google Sheets, klik menu File -> Download -> Microsoft Excel (.xlsx), lalu impor file tersebut di aplikasi.

#### Metode C: Menggunakan Generator 25 Data Simulasi
1. Jika ingin langsung menguji layout sertifikat tanpa file data, buka "Kelola Data Penerima".
2. Klik tombol "Buat 25 Data Simulasi".
3. Aplikasi akan langsung mengisikan 25 data penerima Indonesia dengan variasi gelar akademik panjang, nomor sertifikat, peran, dan instansi untuk menguji ketahanan layout.

#### Metode D: Pengeditan Manual di Aplikasi
- Anda dapat menambah baris baru ("Tambah Baris"), menambah kolom baru ("Tambah Kolom"), mengubah isi sel langsung pada tabel, atau menghapus baris yang tidak diinginkan.
- Klik "Simpan & Terapkan Data ke Sertifikat" untuk menerapkan data ke kanvas pratinjau.

---

### Tahap 3: Mengatur Elemen Teks di Atas Kanvas

1. **Menambah Kotak Teks**:
   - Klik tombol "Tambah Teks" pada toolbar atas untuk meletakkan kotak teks baru di kanvas.
2. **Memasukkan Variabel Dinamis**:
   - Di panel kiri pada kotak "Konten Teks", Anda dapat menuliskan teks tetap digabung dengan placeholder kurung kurawal.
   - Contoh:
     - `Diberikan kepada {nama}`
     - `Nomor: {nomor}`
     - `Sebagai {peran} dalam kegiatan {kegiatan}`
   - Tersedia tombol cepat klik di bawah kotak teks untuk menyisipkan variabel yang tersedia seperti `{nama}`, `{nomor}`, `{peran}`, `{instansi}`, dan `{tanggal}`.
3. **Mengatur Tipografi & Tampilan**:
   - Font Family: Pilih dari seluruh font yang terpasang pada komputer Anda.
   - Ukuran Font: Masukkan angka dalam poin (pt) atau geser slider ukuran font.
   - Gaya Huruf: Tombol tebal (Bold) dan miring (Italic).
   - Warna Huruf: Masukkan kode warna HEX (misal: `#0F2027`, `#B8860B`), pilih dari palet warna elegan sertifikat (Emas, Perunggu, Navy, Marun, Hitam Arang, Emerald), atau klik "Pilih Warna" untuk membuka dialog pemilih warna slider RGB.
   - Perataan Teks: Rata Kiri, Rata Tengah, atau Rata Kanan.
4. **Mengatur Posisi Teks (Drag Mouse & Koordinat mm)**:
   - Geser mouse (Drag and Drop) langsung pada kotak teks di atas kanvas.
   - Koordinat X dan Y (dalam milimeter) di panel kiri akan sinkron secara otomatis.
   - Anda juga dapat mengetikkan angka posisi X (mm) dan Y (mm) secara presisi di panel pengaturan.
5. **Titik Pusat Visual & Garis Pemandu Magnetik (Snap to Center)**:
   - Setiap kali kotak teks dipilih, kanvas menampilkan crosshair dan titik pusat di bagian tengah kotak, serta tanda takik di sisi atas dan bawah.
   - Saat Anda menggeser kotak teks mendekati garis tengah halaman, kotak akan otomatis menempel (snap) ke titik tengah halaman, disertai garis pandu putus-putus merah vertikal atau horizontal dan label penanda posisi.
   - Kotak teks juga dapat menempel otomatis ke garis tengah kotak teks lain di atas atau di bawahnya, memudahkan pembuatan susunan teks yang lurus sejajar.
6. **Tombol Cepat Menengahkan Teks**:
   - Tombol "Tengahkan Horisontal": Memposisikan kotak teks tepat di tengah lebar kertas secara matematis dalam satu klik.
   - Tombol "Tengahkan Vertikal": Memposisikan kotak teks di tengah tinggi kertas.
7. **Menggeser Presisi dengan Tombol Panah Keyboard**:
   - Pilih kotak teks di kanvas, lalu gunakan tombol panah keyboard:
     - Panah Normal (Kiri, Kanan, Atas, Bawah): Geser sejauh 1.0 mm per ketukan.
     - Shift + Panah: Geser cepat sejauh 5.0 mm per ketukan.
     - Ctrl + Panah: Geser mikro presisi tinggi sejauh 0.2 mm per ketukan.
   - Tersedia juga tombol arah visual (D-Pad panah) pada panel samping aplikasi.
8. **Mengatur Batas Lebar Kotak dan Fitur Auto-Shrink**:
   - Tarik pegangan biru di sisi kanan kotak teks untuk memperlebar atau mempersempit batas kotak, atau isi angka "Lebar Kotak (mm)" pada panel kiri.
   - Centang opsi "Auto-shrink font jika teks kepanjangan":
     - Jika nama peserta sangat panjang (misalnya disertai gelar kehormatan dan akademik berlapis), ukuran huruf akan mengecil secara otomatis dan proporsional agar teks tetap muat rapi di dalam batas kotak dan tidak menabrak tepi sertifikat.

---

### Tahap 4: Navigasi dan Pratinjau Data Real-time

Pada baris kontrol navigasi di bagian bawah aplikasi:

- Tombol "Pertama": Menampilkan data penerima nomor 1.
- Tombol "Sebelumnya": Menampilkan data penerima sebelum baris saat ini.
- Indikator Status: Menampilkan "Data X dari Y: [Nama Penerima Saat Ini]".
- Tombol "Berikutnya": Menampilkan data penerima baris berikutnya.
- Tombol "Terakhir": Menampilkan data penerima paling akhir.

Dengan navigasi ini, Anda dapat memeriksa pratinjau tampilan sertifikat untuk setiap peserta secara instan sebelum melakukan proses ekspor massal.

---

### Tahap 5: Ekspor Massal dan Cetak Langsung

#### Ekspor Massal
1. Klik tombol "Ekspor Massal (PDF / PNG)" pada toolbar atas.
2. Dialog ekspor akan terbuka dengan pilihan format:
   - **Satu File PDF Gabungan**: Menggabungkan seluruh sertifikat ke dalam 1 berkas PDF multi-halaman. Sangat praktis untuk dikirimkan ke percetakan.
   - **Berkas Terpisah per Nama (PDF)**: Menghasilkan 1 file PDF untuk setiap penerima dengan penamaan otomatis teratur (contoh: `Budi_Santoso_001.pdf`).
   - **Berkas Terpisah per Nama (PNG 300 DPI)**: Menghasilkan file gambar PNG siap cetak resolusi tinggi untuk setiap penerima. Sangat cocok jika sertifikat ingin dibagikan via WhatsApp atau email peserta.
3. Tentukan nama file atau folder tujuan penyimpanan.
4. Klik "Mulai Ekspor".
5. Proses akan berjalan di latar belakang (background task) tanpa membuat aplikasi macet atau membeku (anti-freeze). Progress bar dan persentase akan menampilkan kemajuan pengerjaan. Tersedia juga tombol "Batal" jika ingin menghentikan proses di tengah jalan.
6. Setelah selesai, klik tombol "Buka Lokasi File" untuk langsung membuka folder hasil ekspor di Windows Explorer.

Catatan Penting Font pada PDF:
Engine render `SkiaSharp` secara otomatis menanamkan subset font (*font embedding*) ke dalam file PDF. Hasil PDF dapat dibuka di komputer, ponsel, atau sistem operasi lain tanpa mengalami perubahan bentuk font meskipun font tersebut tidak terpasang di komputer penerima.

#### Cetak Langsung (Direct Print)
- Jika ingin mencetak lembar sertifikat yang sedang aktif saat ini secara langsung, klik tombol "Cetak Langsung".
- Dialog printer Windows akan terbuka untuk memilih printer fisik atau driver virtual printer (seperti Microsoft Print to PDF).

---

### Tahap 6: Menyimpan dan Membuka Template Desain

Jika Anda telah menyelesaikan pengaturan background dan posisi kotak teks, Anda dapat menyimpannya sebagai template agar dapat digunakan kembali di masa mendatang:

1. **Menyimpan Template**:
   - Klik tombol "Simpan Template" pada toolbar atas.
   - Tentukan lokasi penyimpanan file (format `.json`).
   - File JSON akan menyimpan ukuran kertas, path gambar background, serta semua atribut kotak teks (posisi X, Y, font, ukuran, warna, perataan, lebar kotak, dan status auto-shrink).
2. **Membuka Template**:
   - Klik tombol "Buka Template".
   - Pilih file template JSON yang telah disimpan sebelumnya.
   - Seluruh pengaturan sertifikat akan langsung dimuat kembali ke kanvas secara utuh.

---

## 5. Daftar Pintasan Keyboard (Shortcuts)

| Tombol Pintasan | Fungsi |
| :--- | :--- |
| **Ctrl + Z** | Batalkan aksi terakhir (Undo posisi, teks, penambahan, hapus, duplikat) |
| **Ctrl + Y** atau **Ctrl + Shift + Z** | Ulangi aksi yang dibatalkan (Redo) |
| **Ctrl + X** | Potong kotak teks yang dipilih (Cut) |
| **Ctrl + C** | Salin kotak teks yang dipilih (Copy) |
| **Ctrl + V** | Tempel kotak teks dari clipboard (Paste) |
| **Ctrl + D** | Duplikasi langsung kotak teks yang dipilih (Duplicate) |
| **Delete** | Hapus kotak teks yang dipilih dari kanvas |
| **Panah (← ↑ → ↓)** | Geser posisi kotak teks sejauh 1.0 mm |
| **Shift + Panah** | Geser cepat posisi kotak teks sejauh 5.0 mm |
| **Ctrl + Panah** | Geser mikro presisi tinggi sejauh 0.2 mm |
| **Ctrl + S** | Simpan template sertifikat ke file JSON |
| **Ctrl + O** | Buka template sertifikat dari file JSON |
| **Ctrl + P** | Cetak langsung lembar sertifikat aktif |
| **Ctrl + E** | Buka dialog ekspor massal sertifikat |

---

## 6. Solusi Masalah Umum (Troubleshooting)

1. **Aplikasi Tidak Mau Terbuka saat Double Click .EXE**:
   - Pastikan komputer Anda telah terpasang **.NET Desktop Runtime 10.0**. Jika belum, jalankan installer .NET Desktop Runtime x64 dari situs resmi Microsoft.
   - Pastikan file `CetakSertifikatApp.exe` dijalankan di dalam foldernya bersama file-file `.dll` pendukung dan folder `runtimes`, bukan dipindahkan sendirian ke Desktop. Buat shortcut (*Send to Desktop*) jika ingin mengaksesnya dari Desktop.
2. **Peringatan Gambar di Bawah 150 DPI**:
   - Jika muncul dialog peringatan resolusi gambar rendah, background sertifikat Anda memiliki piksel yang kurang padat untuk ukuran cetak fisik. Disarankan mengekspor ulang desain background dari Canva, Photoshop, atau CorelDraw dengan resolusi minimal 300 DPI atau ukuran piksel sekitar 3508 x 2480 piksel untuk ukuran A4.
3. **Nama Sangat Panjang Terpotong atau Keluar Batas**:
   - Pastikan kotak centang "Auto-shrink font jika teks kepanjangan" dalam keadaan aktif.
   - Pastikan "Lebar Kotak (mm)" cukup memadai dengan menarik pegangan resize biru di sisi kanan kotak teks.
4. **Variabel Tidak Berubah saat Ganti Penerima**:
   - Periksa penulisan nama variabel. Variabel harus ditulis menggunakan kurung kurawal, contoh: `{nama}`.
   - Pastikan nama kolom pada file Excel atau CSV sesuai dengan nama variabel tersebut (pencocokan bersifat *case-insensitive*, `{nama}` sama dengan `{NAMA}` atau `{Nama}`).
5. **Karakter Khusus atau Tanda Baca Gelar Tidak Terbaca di CSV**:
   - Jika menggunakan file CSV, pastikan file disimpan dengan encoding **UTF-8** agar simbol koma, titik, tanda petik, atau karakter khusus pada gelar tidak rusak.

---

## 7. Struktur Proyek dan Teknologi

Aplikasi ini dibangun menggunakan arsitektur MVVM (*Model-View-ViewModel*) yang rapi dan terstruktur:

- **Bahasa Pemrograman**: C# 13 / .NET 10 WPF (Windows Presentation Foundation).
- **Engine Grafis & Dokumen**: SkiaSharp (rendering 2D, font embedding, PDF & PNG high-res export).
- **Pembaca Spreadsheet**: ClosedXML (membaca format Excel `.xlsx` tanpa memerlukan Microsoft Office).
- **Pembaca CSV**: CsvHelper (membaca file CSV dengan deteksi otomatis pemisah koma, titik koma, dan tab).
- **Penyimpanan Template**: System.Text.Json (serialisasi JSON berbasis ukuran milimeter fisik).
- **Testing**: xUnit (dilengkapi 32 unit test yang memvalidasi parsing XAML, SkiaSharp rendering, data import, placeholder replacement, dan auto-shrink calculation).

### Struktur File Utama:

```
CetakSertifikat/
├── CetakSertifikatApp.csproj     # Berkas proyek utama WPF
├── App.xaml / App.xaml.cs        # Titik masuk aplikasi
├── MainWindow.xaml / .cs         # Tampilan jendela utama aplikasi
├── Controls/
│   ├── CertificateCanvas.xaml    # Kanvas interaktif (drag, snap, guidelines, resize)
│   └── CertificateCanvas.xaml.cs # Logika penanganan event mouse dan snapping
├── Models/
│   ├── CertificateTemplate.cs    # Model data template JSON sertifikat
│   ├── TextFieldDefinition.cs    # Model definisi atribut kotak teks (mm)
│   ├── RecipientRecord.cs        # Model baris data penerima & mesin pengganti variabel
│   └── PageSizePreset.cs         # Preset ukuran kertas standar
├── Services/
│   ├── CertificateRenderer.cs    # Mesin render SkiaSharp untuk PDF dan PNG
│   ├── DataImportService.cs      # Layanan pembaca Excel, CSV, clipboard, dan mock generator
│   ├── ExportService.cs          # Pengelola proses ekspor massal di latar belakang
│   ├── FontService.cs            # Layanan pembacaan font sistem
│   └── TemplateService.cs        # Layanan penyimpanan dan pembacaan template JSON
├── ViewModels/
│   ├── MainViewModel.cs          # ViewModel utama pengendali alur aplikasi
│   └── FieldViewModel.cs         # ViewModel kotak teks (sinkronisasi mm ke piksel kanvas)
├── Views/
│   ├── DataEditorDialog.xaml     # Dialog kelola data, download template, dan mock generator
│   ├── GoogleFormGuideDialog.xaml # Dialog panduan visual Google Forms
│   ├── AddColumnDialog.xaml      # Dialog penambahan kolom kustom
│   ├── ExportDialog.xaml         # Dialog konfigurasi ekspor massal dan progress bar
│   └── InputColorDialog.xaml     # Dialog pemilih warna teks
├── Resources/
│   └── Icons.xaml                # Pustaka icon vektor geometris aplikasi
└── Tests/
    ├── CetakSertifikatApp.Tests.csproj # Proyek pengujian unit test
    └── CertificateAppTests.cs          # 32 pengujian otomatis
```
