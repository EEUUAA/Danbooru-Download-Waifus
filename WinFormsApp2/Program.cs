using System.Text.Json;
using System.Text.Json.Serialization;

namespace DanbooruApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public partial class Form1 : Form
    {
        private TextBox inputTextBox;
        private TextBox limitTextBox;
        private Button searchButton;
        private FlowLayoutPanel imagePanel;
        private CheckBox listOnlyCheckBox; 
        private CheckBox downloadAllCheckBox;
        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.inputTextBox = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Enter tags (e.g., 'cat_ears')" };
            this.limitTextBox = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Enter limit (default: 1)", Text = "1" };
            this.listOnlyCheckBox = new CheckBox { Text = "List URLs Only", Dock = DockStyle.Top };
            this.searchButton = new Button { Text = "Search", Dock = DockStyle.Top };
            this.imagePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
            this.downloadAllCheckBox = new CheckBox { Text = "Download All", Dock = DockStyle.Top, Checked = false };

            this.searchButton.Click += async (sender, e) => await SearchImagesAsync();

            this.Controls.Add(this.imagePanel);
            this.Controls.Add(this.searchButton);
            this.Controls.Add(this.downloadAllCheckBox);
            this.Controls.Add(this.listOnlyCheckBox);
            this.Controls.Add(this.limitTextBox);
            this.Controls.Add(this.inputTextBox);

            this.Text = "Danbooru Image Viewer";
            this.ClientSize = new System.Drawing.Size(800, 600);
        }

        private async Task SearchImagesAsync()
        {
            string tags = inputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(tags))
            {
                MessageBox.Show("Please enter some tags.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(limitTextBox.Text.Trim(), out int totalLimit) || totalLimit <= 0)
            {
                MessageBox.Show("Please enter a valid positive number for the limit.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            searchButton.Enabled = false;
            searchButton.Text = "Loading...";
            imagePanel.Controls.Clear();

            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "GetMeWaifusPlease/1.0"); //here you are danbooru, please know it
                int loadedImages = 0;
                int page = 1;
                const int maxPageLimit = 200;

                string? downloadFolder = null;
                if (downloadAllCheckBox.Checked)
                {
                    using FolderBrowserDialog folderDialog = new FolderBrowserDialog
                    {
                        Description = "Select a folder to save images"
                    };

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        downloadFolder = folderDialog.SelectedPath;
                    }
                    else
                    {
                        searchButton.Enabled = true;
                        searchButton.Text = "Search";
                        return;
                    }
                }

                while (loadedImages < totalLimit)
                {
                    int currentLimit = Math.Min(maxPageLimit, totalLimit - loadedImages);
                    string apiUrl = $"https://danbooru.donmai.us/posts.json?tags={Uri.EscapeDataString(tags)}&limit={currentLimit}&page={page}";
                    string response = await client.GetStringAsync(apiUrl);

                    var posts = JsonSerializer.Deserialize<Post[]>(response);
                    if (posts == null || posts.Length == 0) break;

                    foreach (var post in posts)
                    {
                        if (!string.IsNullOrEmpty(post.PreviewUrl) && !string.IsNullOrEmpty(post.FileUrl))
                        {
                            if (downloadAllCheckBox.Checked && downloadFolder != null)
                            {
                                try
                                {
                                    string fileName = Path.GetFileName(new Uri(post.FileUrl).LocalPath);
                                    string filePath = Path.Combine(downloadFolder, fileName);

                                    using HttpClient downloadClient = new HttpClient();
                                    downloadClient.DefaultRequestHeaders.Add("User-Agent", "WaifuComeToMe/1.0");//here you are danbooru, please know it!!!!!!!!!!!!!!!
                                    downloadClient.DefaultRequestHeaders.Add("Referer", "https://danbooru.donmai.us/");
                                    using var highResResponse = await downloadClient.GetAsync(post.FileUrl);
                                    highResResponse.EnsureSuccessStatusCode();

                                    using var highResStream = await highResResponse.Content.ReadAsStreamAsync();
                                    using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                                    await highResStream.CopyToAsync(fileStream);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Failed to download image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else if (!listOnlyCheckBox.Checked)
                            {
                                try
                                {
                                    using var previewResponse = await client.GetAsync(post.PreviewUrl);
                                    previewResponse.EnsureSuccessStatusCode();

                                    using var previewStream = await previewResponse.Content.ReadAsStreamAsync();
                                    var pictureBox = new PictureBox
                                    {
                                        Image = System.Drawing.Image.FromStream(previewStream),
                                        SizeMode = PictureBoxSizeMode.Zoom,
                                        Width = 180,
                                        Height = 180,
                                        Margin = new Padding(5),
                                        Cursor = Cursors.Hand,
                                        Tag = post.FileUrl
                                    };

                                    pictureBox.Click += async (sender, e) =>
                                    {
                                        string highResUrl = pictureBox.Tag as string;
                                        if (string.IsNullOrEmpty(highResUrl))
                                        {
                                            MessageBox.Show("High-resolution URL is missing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            return;
                                        }

                                        using SaveFileDialog saveFileDialog = new SaveFileDialog
                                        {
                                            Filter = "Image Files|*.jpg;*.png;*.bmp",
                                            Title = "Save High-Resolution Image"
                                        };

                                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                                        {
                                            try
                                            {
                                                using HttpClient downloadClient = new HttpClient();
                                                downloadClient.DefaultRequestHeaders.Add("User-Agent", "WaifuIsMine/1.0"); // iwuiu save
                                                downloadClient.DefaultRequestHeaders.Add("Referer", "https://danbooru.donmai.us/");
                                                using var highResResponse = await downloadClient.GetAsync(highResUrl);
                                                highResResponse.EnsureSuccessStatusCode();

                                                using var highResStream = await highResResponse.Content.ReadAsStreamAsync();
                                                using var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write);
                                                await highResStream.CopyToAsync(fileStream);

                                                MessageBox.Show("Image saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            }
                                            catch (Exception saveEx)
                                            {
                                                MessageBox.Show($"Failed to save image: {saveEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                        }
                                    };

                                    imagePanel.Controls.Add(pictureBox);
                                }
                                catch (Exception loadEx)
                                {
                                    MessageBox.Show($"Failed to load preview image: {loadEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }

                            loadedImages++;
                            if (loadedImages >= totalLimit) break;
                        }
                    }

                    if (posts.Length < currentLimit) break;
                    page++;
                }

                if (loadedImages == 0)
                {
                    MessageBox.Show("No images found for the given tags.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.Text = $"Danbooru Image Viewer - {loadedImages} {(listOnlyCheckBox.Checked ? "URLs" : "Preview Images")}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                searchButton.Enabled = true;
                searchButton.Text = "Search";
            }
        }

        private class Post
        {
            [JsonPropertyName("file_url")]
            public string FileUrl { get; set; } = string.Empty;

            [JsonPropertyName("preview_file_url")]
            public string PreviewUrl { get; set; } = string.Empty;
        }
    }
}
