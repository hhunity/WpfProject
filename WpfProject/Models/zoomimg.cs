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



