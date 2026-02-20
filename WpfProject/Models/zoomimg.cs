private void panelView_MouseMove(object sender, MouseEventArgs e)
{
    if (image == null) return;

    int imgX = viewX + e.X / zoom;
    int imgY = viewY + e.Y / zoom;

    // 範囲チェック
    if (imgX < 0 || imgY < 0 || imgX >= image.Width || imgY >= image.Height)
    {
        labelCoord.Text = "(out)";
        return;
    }

    labelCoord.Text = $"X={imgX}, Y={imgY}";
}

private unsafe bool GetBit1bpp(Bitmap bmp, int x, int y)
{
    var rect = new Rectangle(0, y, bmp.Width, 1);
    var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);

    try
    {
        byte* row = (byte*)data.Scan0;
        int byteIndex = x >> 3;
        int bitIndex = 7 - (x & 7);

        return ((row[byteIndex] >> bitIndex) & 1) != 0;
    }
    finally
    {
        bmp.UnlockBits(data);
    }
}

bool bit = GetBit1bpp(image1, imgX, imgY);

labelCoord.Text = $"X={imgX}, Y={imgY}, Val={(bit ? 1 : 0)}";




private void SetZoomAtPoint(int newZoom, int mouseX, int mouseY)
{
    if (image == null) return;
    if (newZoom < 1) newZoom = 1;

    // 画面座標(mouseX,mouseY)が指している画像座標（ズーム前）
    double anchorImgX = viewX + (double)mouseX / zoom;
    double anchorImgY = viewY + (double)mouseY / zoom;

    // ズーム更新
    zoom = newZoom;

    // ズーム後も同じ画像座標が mouseX/mouseY に来るように viewX/Y を調整
    viewX = (int)Math.Round(anchorImgX - (double)mouseX / zoom);
    viewY = (int)Math.Round(anchorImgY - (double)mouseY / zoom);

    ClampView();
    UpdateScrollbarsFromView();
    panelView.Invalidate();
}

private void panelView_MouseWheel(object sender, MouseEventArgs e)
{
    if (image == null) return;

    // 例：Shift+Wheel でズーム（Ctrlは水平移動に使ってる前提）
    bool shift = (Control.ModifierKeys & Keys.Shift) != 0;
    bool ctrl  = (Control.ModifierKeys & Keys.Control) != 0;

    if (shift)
    {
        int newZoom = zoom;

        if (e.Delta > 0) newZoom = NextZoomIn(zoom);   // 1->2->4...
        else             newZoom = NextZoomOut(zoom);  // 4->2->1...

        SetZoomAtPoint(newZoom, e.X, e.Y);
        return;
    }

    // 既存仕様：Ctrl+Wheelで水平、通常で垂直
    ScrollByWheel(e);
}




public Form1()
{
    InitializeComponent();

    panelView.TabStop = true;                 // これが重要
    panelView.MouseEnter += (_, __) => panelView.Focus();
    panelView.MouseDown  += (_, __) => panelView.Focus();

    panelView.MouseWheel += panelView_MouseWheel;
}

private void panelView_MouseWheel(object sender, MouseEventArgs e)
{
    if (image == null) return;

    // 1ノッチあたりの移動量（好みで調整）
    int step = 120; // e.Deltaは通常±120単位
    int lines = e.Delta / step; // 上: +1, 下: -1

    // 画像座標で何px動かすか（ズーム倍率を考慮）
    // 「画面上で一定量」動かしたいなら /zoom は入れない
    int movePx = 200 / zoom;  // 調整ポイント（例：200px相当）
    if (movePx < 1) movePx = 1;

    bool ctrl = (Control.ModifierKeys & Keys.Control) != 0;

    if (ctrl)
        viewX -= lines * movePx;   // Ctrl+Wheel: 水平（好みで符号調整）
    else
        viewY -= lines * movePx;   // Wheel: 垂直

    ClampView();
    UpdateScrollbarsFromView();
    panelView.Invalidate();
}
private void ClampView()
{
    if (image == null) return;

    int viewWImg = Math.Max(1, panelView.ClientSize.Width / zoom);
    int viewHImg = Math.Max(1, panelView.ClientSize.Height / zoom);

    int maxX = Math.Max(0, image.Width  - viewWImg);
    int maxY = Math.Max(0, image.Height - viewHImg);

    viewX = Clamp(viewX, 0, maxX);
    viewY = Clamp(viewY, 0, maxY);
}
private void UpdateScrollbarsFromView()
{
    if (image == null) return;

    updatingScrollbars = true;
    try
    {
        hScroll.Value = ImageXToScroll(viewX);
        vScroll.Value = ImageYToScroll(viewY);
    }
    finally
    {
        updatingScrollbars = false;
    }
}






using System.Drawing;
using System.Drawing.Imaging;

private Bitmap view8;

private void EnsureView8(int w, int h)
{
    if (view8 != null && view8.Width == w && view8.Height == h) return;

    view8?.Dispose();
    view8 = new Bitmap(w, h, PixelFormat.Format8bppIndexed);

    // グレーパレット（0..255）
    var pal = view8.Palette;
    for (int i = 0; i < 256; i++)
        pal.Entries[i] = Color.FromArgb(i, i, i);
    view8.Palette = pal;
}

private static void Expand1bppTo8bpp(Bitmap src1, Rectangle srcRect, Bitmap dst8)
{
    // src1: Format1bppIndexed, dst8: Format8bppIndexed, dst8サイズ==srcRect.Size
    var dstRect = new Rectangle(0, 0, srcRect.Width, srcRect.Height);

    var sData = src1.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
    var dData = dst8.LockBits(dstRect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

    try
    {
        unsafe
        {
            byte* sBase = (byte*)sData.Scan0;
            byte* dBase = (byte*)dData.Scan0;

            int w = srcRect.Width;
            int h = srcRect.Height;

            // srcRect.X は LockBits の矩形で切られているので、各行先頭は bit0 から始まる
            // つまり、ここでは "行内ビットオフセット" を気にしなくてOK（srcRectで切っているため）
            // ただし、GDI+のLockBitsは矩形切り出しでも Scan0 はその矩形の先頭ビットを指す（一般にOK）

            for (int y = 0; y < h; y++)
            {
                byte* sp = sBase + y * sData.Stride;
                byte* dp = dBase + y * dData.Stride;

                int x = 0;
                int byteCount = (w + 7) / 8;

                for (int i = 0; i < byteCount; i++)
                {
                    byte b = sp[i];

                    // 1bppは通常 MSBが左（bit7→x）
                    // x..x+7 を埋める（最後の端は幅で止める）
                    for (int bit = 7; bit >= 0 && x < w; bit--)
                    {
                        dp[x] = ((b >> bit) & 1) != 0 ? (byte)255 : (byte)0;
                        x++;
                    }
                }
            }
        }
    }
    finally
    {
        src1.UnlockBits(sData);
        dst8.UnlockBits(dData);
    }
}

private Bitmap image1; // Format1bppIndexed
private int zoom = 1;
private int viewX = 0, viewY = 0;

private void panelView_Paint(object sender, PaintEventArgs e)
{
    if (image1 == null) return;

    // 可視領域（画像座標での幅高さ）
    int viewWImg = Math.Max(1, panelView.ClientSize.Width / zoom);
    int viewHImg = Math.Max(1, panelView.ClientSize.Height / zoom);

    int maxX = Math.Max(0, image1.Width - viewWImg);
    int maxY = Math.Max(0, image1.Height - viewHImg);
    viewX = Clamp(viewX, 0, maxX);
    viewY = Clamp(viewY, 0, maxY);

    var src = new Rectangle(viewX, viewY,
        Math.Min(viewWImg, image1.Width - viewX),
        Math.Min(viewHImg, image1.Height - viewY));

    EnsureView8(src.Width, src.Height);

    // ★ 1bpp→8bppに展開（ここで32000超え問題を回避しやすい）
    Expand1bppTo8bpp(image1, src, view8);

    // 描画（整数倍 + 最近傍）
    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
    e.Graphics.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.Half;
    e.Graphics.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.None;
    e.Graphics.CompositingQuality= System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

    var dst = new Rectangle(0, 0, src.Width * zoom, src.Height * zoom);
    e.Graphics.DrawImage(view8, dst, new Rectangle(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);
}





private void panelView_Paint(object sender, PaintEventArgs e)
{
    if (image == null) return;

    ClampView();

    int viewWImg = Math.Max(1, panelView.ClientSize.Width / zoom);
    int viewHImg = Math.Max(1, panelView.ClientSize.Height / zoom);

    // 表示したいsrc（画像座標）
    var src = new Rectangle(viewX, viewY,
        Math.Min(viewWImg, image.Width - viewX),
        Math.Min(viewHImg, image.Height - viewY));

    EnsureViewBuffer(src.Width, src.Height);

    // ★ ここで大きい座標から小さいBitmapへコピー（DrawImageの不具合回避）
    CopyRegion(image, src, viewBmp);

    // 描画（座標小さいので安定）
    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
    e.Graphics.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.Half;
    e.Graphics.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.None;
    e.Graphics.CompositingQuality= System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

    var dst = new Rectangle(0, 0, src.Width * zoom, src.Height * zoom);
    e.Graphics.DrawImage(viewBmp, dst, new Rectangle(0,0,src.Width,src.Height), GraphicsUnit.Pixel);
}

private void EnsureViewBuffer(int w, int h)
{
    if (viewBmp != null && viewBmp.Width == w && viewBmp.Height == h) return;

    viewBmp?.Dispose();
    viewBmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
}
using System;
using System.Drawing;
using System.Drawing.Imaging;

private static void CopyRegion(Bitmap srcBmp, Rectangle srcRect, Bitmap dstBmp)
{
    // dstBmp は srcRect.Size と同じであること
    var dstRect = new Rectangle(0, 0, srcRect.Width, srcRect.Height);

    var srcData = srcBmp.LockBits(srcRect, ImageLockMode.ReadOnly, srcBmp.PixelFormat);
    var dstData = dstBmp.LockBits(dstRect, ImageLockMode.WriteOnly, dstBmp.PixelFormat);

    try
    {
        int bytesPerPixel = Image.GetPixelFormatSize(srcBmp.PixelFormat) / 8;
        if (bytesPerPixel != Image.GetPixelFormatSize(dstBmp.PixelFormat) / 8)
            throw new InvalidOperationException("PixelFormat must match between src and dst.");

        int copyBytesPerRow = srcRect.Width * bytesPerPixel;

        unsafe
        {
            byte* srcBase = (byte*)srcData.Scan0;
            byte* dstBase = (byte*)dstData.Scan0;

            for (int y = 0; y < srcRect.Height; y++)
            {
                Buffer.MemoryCopy(
                    srcBase + y * srcData.Stride,
                    dstBase + y * dstData.Stride,
                    dstData.Stride,
                    copyBytesPerRow);
            }
        }
    }
    finally
    {
        srcBmp.UnlockBits(srcData);
        dstBmp.UnlockBits(dstData);
    }
}







private Bitmap image;     // 表示したい画像
private int zoom = 1;     // 1,2,3,4...（整数倍）
private int viewX = 0;    // 表示左上（画像座標, px）
private int viewY = 0;


public Form1()
{
    InitializeComponent();

    panelView.Paint += panelView_Paint;
    panelView.Resize += (_, __) => { UpdateScrollbars(); panelView.Invalidate(); };

    hScroll.Scroll += (_, __) => { viewX = ScrollToImageX(hScroll.Value); panelView.Invalidate(); };
    vScroll.Scroll += (_, __) => { viewY = ScrollToImageY(vScroll.Value); panelView.Invalidate(); };
}

private const int ScrollMax = 100000;

private void UpdateScrollbars()
{
    if (image == null) return;

    int viewWImg = Math.Max(1, panelView.ClientSize.Width / zoom);
    int viewHImg = Math.Max(1, panelView.ClientSize.Height / zoom);

    int maxX = Math.Max(0, image.Width - viewWImg);
    int maxY = Math.Max(0, image.Height - viewHImg);

    // スクロール範囲が無いなら無効化
    hScroll.Enabled = maxX > 0;
    vScroll.Enabled = maxY > 0;

    hScroll.Minimum = 0;
    vScroll.Minimum = 0;

    hScroll.Maximum = ScrollMax;
    vScroll.Maximum = ScrollMax;

    hScroll.LargeChange = Math.Max(1, ScrollMax / 10);
    vScroll.LargeChange = Math.Max(1, ScrollMax / 10);

    hScroll.Value = ImageXToScroll(viewX);
    vScroll.Value = ImageYToScroll(viewY);
}

private int ImageXToScroll(int x)
{
    int viewWImg = Math.Max(1, panelView.ClientSize.Width / zoom);
    int maxX = Math.Max(0, image.Width - viewWImg);
    if (maxX == 0) return 0;

    long v = (long)x * ScrollMax / maxX;
    return (int)Math.Clamp(v, 0, ScrollMax);
}

private int ImageYToScroll(int y)
{
    int viewHImg = Math.Max(1, panelView.ClientSize.Height / zoom);
    int maxY = Math.Max(0, image.Height - viewHImg);
    if (maxY == 0) return 0;

    long v = (long)y * ScrollMax / maxY;
    return (int)Math.Clamp(v, 0, ScrollMax);
}

private int ScrollToImageX(int s)
{
    int viewWImg = Math.Max(1, panelView.ClientSize.Width / zoom);
    int maxX = Math.Max(0, image.Width - viewWImg);
    if (maxX == 0) return 0;

    long x = (long)s * maxX / ScrollMax;
    return (int)Math.Clamp(x, 0, maxX);
}

private int ScrollToImageY(int s)
{
    int viewHImg = Math.Max(1, panelView.ClientSize.Height / zoom);
    int maxY = Math.Max(0, image.Height - viewHImg);
    if (maxY == 0) return 0;

    long y = (long)s * maxY / ScrollMax;
    return (int)Math.Clamp(y, 0, maxY);
}

private void LoadImage(string path)
{
    image?.Dispose();
    image = (Bitmap)Bitmap.FromFile(path);

    zoom = 1;
    viewX = viewY = 0;

    UpdateScrollbars();
    panelView.Invalidate();
}

private void btn1x_Click(object sender, EventArgs e) { SetZoom(1); }
private void btn2x_Click(object sender, EventArgs e) { SetZoom(2); }
private void btn4x_Click(object sender, EventArgs e) { SetZoom(4); }

private void SetZoom(int newZoom)
{
    if (image == null) return;
    if (newZoom < 1) newZoom = 1;

    // 可能なら画面中心を維持（簡易）
    int cxImg = viewX + panelView.ClientSize.Width / (2 * zoom);
    int cyImg = viewY + panelView.ClientSize.Height / (2 * zoom);

    zoom = newZoom;

    viewX = cxImg - panelView.ClientSize.Width / (2 * zoom);
    viewY = cyImg - panelView.ClientSize.Height / (2 * zoom);

    ClampView();
    UpdateScrollbars();
    panelView.Invalidate();
}

private void panelView_Paint(object sender, PaintEventArgs e)
{
    if (image == null) return;

    ClampView();

    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
    e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
    e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

    int viewWImg = Math.Max(1, panelView.ClientSize.Width / zoom);
    int viewHImg = Math.Max(1, panelView.ClientSize.Height / zoom);

    var src = new Rectangle(viewX, viewY,
        Math.Min(viewWImg, image.Width - viewX),
        Math.Min(viewHImg, image.Height - viewY));

    var dst = new Rectangle(0, 0, src.Width * zoom, src.Height * zoom);

    e.Graphics.DrawImage(image, dst, src, GraphicsUnit.Pixel);
}

private void ClampView()
{
    if (image == null) return;

    int viewWImg = Math.Max(1, panelView.ClientSize.Width / zoom);
    int viewHImg = Math.Max(1, panelView.ClientSize.Height / zoom);

    int maxX = Math.Max(0, image.Width - viewWImg);
    int maxY = Math.Max(0, image.Height - viewHImg);

    viewX = Math.Clamp(viewX, 0, maxX);
    viewY = Math.Clamp(viewY, 0, maxY);
}



