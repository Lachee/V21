using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImageMagick;

namespace V21Bot.Magicks
{
	class TriggeredMagick : IMagick
	{
		const string TRIGGER = "triggeredbarx256.png";

		public int FrameCount { get; set; } = 4;
		public int FrameDelay { get; set; } = 2;
		public int Colours { get; set; } = 64;
		public int Size { get; set; } = 128;
		public int TotalTypes { get; set; } = 3;

		private bool _enableAlpha = false;
		private Random _rnd;

		public TriggeredMagick()
		{
			_rnd = new Random();
		}

		public string Name => "triggered";
		public string GetFilename(string username) { return this.Name + "-" + username + ".gif"; }
		public byte[] Generate(string resources, MagickImage image)
		{
			int xseed = _rnd.Next();
			int yseed = _rnd.Next();
			double xscale = _rnd.NextDouble() + 0.5f;
			double yscale = _rnd.NextDouble() + 0.5f;

			//Figure out the type of movement each axis will get.
			int xtype = _rnd.Next(TotalTypes);
			int ytype = 0; do { ytype = _rnd.Next(TotalTypes); } while (ytype == xtype);

			//Movement
			int movement = (int)Math.Ceiling(Size * 0.02734375) + 1;
			int paddingx2 = movement * 2;
			
			//Prepare some things with the base image
			double imageYRatio = (double)image.Height / (double)image.Width;
			int width = Size;
			int height = (int)Math.Floor(width * imageYRatio);

			//Crop the image and scale it
			image.Scale(width + paddingx2, height + paddingx2);
			int baseWidth = width + paddingx2;
			int baseHeight = height + paddingx2;

			//Should we use alpha?
			_enableAlpha = image.HasAlpha;

			//Get the TRIGGERED bar and include that too
			using (var triggerImage = new MagickImage(Path.Combine(resources, TRIGGER)))
			{
				//Scale the triggered image to the correct size
				double triggerImageYRatio = (double)triggerImage.Height / (double)triggerImage.Width;
				int triggerWidth = width;
				int triggerHeight = (int)Math.Floor(triggerWidth * triggerImageYRatio);
				triggerImage.Scale(triggerWidth, triggerHeight);

				//Create a new collection, and add frames too it
				using (var collection = new MagickImageCollection())
				{
					for (int i = 0; i < FrameCount; i++)
					{
						//Create a new image
						MagickImage img;
						if (_enableAlpha)
						{
							img = new MagickImage(MagickColor.FromRgba(0, 255, 255, 0), width, height)
							{
								AnimationDelay = FrameDelay,
								GifDisposeMethod = GifDisposeMethod.Background,
								HasAlpha = true
							};
						}
						else
						{
							img = new MagickImage(MagickColor.FromRgb(0, 255, 255), width, height)
							{
								AnimationDelay = FrameDelay,
								GifDisposeMethod = GifDisposeMethod.None
							};
						}

						//Calculate the offset
						var xOffset = GetOffset((xseed + i) * xscale, xtype, movement - 1);
						var yOffset = GetOffset((yseed + i) * yscale, ytype, movement - 1);

						var x = movement + xOffset;
						var y = movement + yOffset;

						//Copy the base image, offseting it
						img.CopyPixels(image, new MagickGeometry(x, y, width, height), 0, 0, Channels.All);

						//Copy the triggered, moving it to the bottom
						img.CopyPixels(triggerImage, new MagickGeometry(triggerWidth, triggerHeight), 0, height - triggerHeight, Channels.All);

						//Add the image
						collection.Add(img);
					}

					if (!_enableAlpha)
					{
						// Optionally reduce colors
						QuantizeSettings settings = new QuantizeSettings() { Colors = Colours };
						collection.Quantize(settings);
					}

					//Optionally optimize the images (images should have the same size).
					//collection.Optimize();
					collection.RePage();
					byte[] bytes = collection.ToByteArray(MagickFormat.Gif);
					return bytes;
				}
			}
		}

		private int GetOffset(double t, int type, double scale = 5)
		{
			double result = 0;
			switch (type)
			{
				default:
					result = Math.Sin(t);
					break;

				case 1:
					result = Math.Cos(t);
					break;

				case 2:
					result = Math.Tan(t);
					break;

				case 3:
					result = Math.Sinh(t);
					break;

				case 4:
					result = Math.Cosh(t);
					break;

				case 5:
					result = Math.Tanh(t);
					break;
			}

			return (int)Math.Clamp(Math.Round(result * 1000), -scale, scale);
		}
	}
}
