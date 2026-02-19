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



