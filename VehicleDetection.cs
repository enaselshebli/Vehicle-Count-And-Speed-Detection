using Accord.Video.FFMPEG;
using Accord.Video.DirectShow;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Accord.Imaging;
using Accord.Imaging.Filters;
using System.Collections.Generic;
using Accord.Controls;
using Accord.Vision.Detection;
using Accord.Vision.Tracking;

namespace Vehicle_Detection
{
    struct VehicleTracker
    {
        public VehicleTracker(int vehicleCountNumber, int indication, Rectangle rectangle)
        {
            Detector = new Camshift(rectangle);
            VehicleCountNumber = vehicleCountNumber;
            VehicleStartingCount = indication;
            Detector.Conservative = false;
            Detector.Smooth = true;
        }

        ///<summary>
        ///The counting number starting from (1) of the vehicles.
        ///</summary>
        public int VehicleCountNumber;

        /// <summary>
        /// The frame indication after the first vehicle is detected.
        /// </summary>
        public int VehicleStartingCount;

        ///<summary>
        ///The tracker that keeps detecting the vehicle.
        ///</summary>
        public Camshift Detector;
    }

    /// <summary>
    /// The main application the detect the vehicles.
    /// </summary>
    public partial class VehicleDetection : Form
    {
        // List of vehicle trackers that detects the vehicles in the frame
        private List<VehicleTracker> vehicleTrackers = new List<VehicleTracker>();

        // The indication number of the last tracked vehicle
        private int vehicleIndiciationNumber = 0;

        // The indication number of the current frame 
        private int frameIndicationNumber = 0;
        
        // The last detected frame in the video
        private Bitmap lastDetectedFrame;

        // Track video 
        private const bool TrackVideo = true;
        public VehicleDetection()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Read from the set camera.
        /// </summary>
        private void ReadFromCamera()
        {
            var filterInfo = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            var cameraName = (from dev in filterInfo select dev).FirstOrDefault();

            var cameraCapture = new VideoCaptureDevice(cameraName.MonikerString);

            cameraCapture.VideoResolution = (from videoCap in cameraCapture.VideoCapabilities
                where videoCap.FrameSize.Width == 1280
                select videoCap).First();
            videoPlayer.VideoSource = cameraCapture;

        }

        /// <summary>
        /// Read from the video player from disk.
        /// </summary>
        /// <param name="videoFileName"></param>
        private void ReadVideo(string videoFileName)
        {
            var file = new Accord.Video.FFMPEG.VideoFileSource(videoFileName);

            videoPlayer.VideoSource = file;
        }


        /// <summary>
        /// This function is created to tag the vehicles
        /// </summary>
        /// <param name="indicationNumber">The number of the detected vehicle in the video frame.</param>
        /// <param name="rectangle">The surrounding box drawn around the vehicle.</param>
        /// <param name="videoFrame">The current frame detected from the video.</param>
        private void VehicleTag(int indicationNumber, Rectangle rectangle, Bitmap videoFrame)
        {
            using (Graphics graphics = Graphics.FromImage(videoFrame))
            {
                string tagName = $"Vehicle {indicationNumber}";

                Font font = new Font("family", 15, GraphicsUnit.Pixel);

                graphics.Clear(Color.RosyBrown);

                Brush brush = new SolidBrush(Color.RosyBrown);

                var cenr = rectangle.Center();

                var measureString = graphics.MeasureString(tagName, font);

                graphics.FillRectangle(new SolidBrush(color: Color.Aqua), cenr.X, cenr.Y, measureString.Width, measureString.Height);
            }
        }

        /// <summary>
        /// Called when Vehicle Detection starts loading.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CallVehicleDetecor(object sender, EventArgs args)
        {
            if(TrackVideo)
                ReadVideo("./video.mp4");

            videoPlayer.Start(); 
            timer.Start();
        }

        /// <summary>
        /// Called when Vehicle Detection close.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void StopVehicleDetecor(object sender, FormClosedEventArgs args)
        {
            videoPlayer.Stop();
        }


        private void CallNewFrame(object sender, ref Bitmap frame)
        {
            if (lastDetectedFrame != null)
            {
                ThresholdedEuclideanDifference thresholdedEuclideanDifference = new ThresholdedEuclideanDifference(40);

                thresholdedEuclideanDifference.OverlayImage = lastDetectedFrame;

                var diff = thresholdedEuclideanDifference.Apply(frame);


                var blob = new BlobsFiltering();

                blob.CoupledSizeFiltering = true;
                blob.MinHeight = 50;
                blob.MinWidth = 50;

                blob.ApplyInPlace(diff);

                thresholdedBox.Image = diff.Clone() as Bitmap;

                var mask = new ApplyMask(diff);

                // use this as a mask for the current frame
                var maskedBitmap = mask.Apply(frame);

                // put this image in the masked picturebox
                maskedBox.Image = maskedBitmap.Clone() as Bitmap;

                // now find all moving blobs
                if (frameIndicationNumber % 10 == 0)
                {
                    var blobCounter = new BlobCounter();

                    blobCounter.ProcessImage(diff);


                    // only keep blobs that:
                    //     - do not overlap with known cars
                    //     - do not overlap with other blobs 
                    //     - have crossed the middle of the frame
                    //     - are at least 100 pixels tall

                    var blobs = blobCounter.GetObjectsRectangles();
                    var newOtherBlobs = from rec in blobCounter.GetObjectsRectangles()
                        where !vehicleTrackers.Any(t => t.Detector.TrackingObject.Rectangle.IntersectsWith(rec))
                              && !blobs.Any(b => b.IntersectsWith(rec) && b != rec)
                              && rec.Top >= 240 && rec.Bottom <= 480
                              && rec.Height >= 100
                        select rec;

                    foreach (var rect in newOtherBlobs)
                    {
                        vehicleTrackers.Add(new VehicleTracker(++vehicleIndiciationNumber, frameIndicationNumber, rect));
                    }

                    vehicleTrackers.RemoveAll(tracker => tracker.Detector.TrackingObject.Rectangle.Height > 360);

                    vehicleTrackers.RemoveAll(trackers => frameIndicationNumber - trackers.VehicleStartingCount > 30);

                    var image = UnmanagedImage.FromManagedImage(maskedBitmap);
                    vehicleTrackers.ForEach(trackers=> trackers.Detector.ProcessFrame(image));

                    lastDetectedFrame.Dispose();
                    lastDetectedFrame = frame.Clone() as Bitmap;

                    var outputFrame = frame.Clone() as Bitmap;

                    vehicleTrackers.FindAll(trackers => !trackers.Detector.TrackingObject.IsEmpty).ForEach(tracker => VehicleTag(tracker.VehicleStartingCount, tracker.Detector.TrackingObject.Rectangle,
                      outputFrame));

                    frame = outputFrame;
                }

                else 
                    lastDetectedFrame = frame.Clone() as Bitmap;

                frameIndicationNumber++;
            }
        }

        private void TimerCount(object sender, EventArgs e)
        {
            carLabel.Text = $"Vehicles count: {vehicleIndiciationNumber}";
        }

        private void VehicleDetection_Load(object sender, EventArgs e)
        {

        }
    }
}
