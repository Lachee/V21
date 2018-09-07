using ImageMagick;
using System;
using System.Collections.Generic;
using System.Text;

namespace V21Bot.Magicks
{
	class PatsMagick : IMagick
	{
		const string HANS = "Resources/hans.png";


		public int FrameCount { get; set; } = 20;
		public int FrameDelay { get; set; } = 10;

		public int Width { get; set; } = 411;
		public int Height { get; set; } = 375;
		public int Colours { get; set; } = 64;

		private bool _enableAlpha = true;

		public string Name => "pats";
		public string GetFilename(string username) { return this.Name + "-" + username + ".gif"; }
		public byte[] Generate(MagickImage image)
		{
			//Scale the image first to confine to a 256x256
			image.Scale(256, 256);
			image.RePage();

			//Get the TRIGGERED bar and include that too
			using (var hans = new MagickImage(HANS))
			{
				//Create a new collection, and add frames too it
				using (var collection = new MagickImageCollection())
				{
					for (int i = 0; i < FrameCount; i++)
					{
						//Create a new image
						MagickImage img;
						if (_enableAlpha)
						{
							img = new MagickImage(MagickColor.FromRgba(255, 255, 255, 0), Width, Height)
							{
								AnimationDelay = FrameDelay,
								GifDisposeMethod = GifDisposeMethod.Background,
								HasAlpha = true
							};
						}
						else
						{
							img = new MagickImage(MagickColor.FromRgb(255, 255, 255), Width, Height)
							{
								AnimationDelay = FrameDelay,
								GifDisposeMethod = GifDisposeMethod.None
							};
						}


						//Calcaulte the y
						//MAX 60
						double sin = Math.Sin((Math.PI * 2.0) * (double)(i / (double)FrameCount));
						sin = (sin + 1) * 30;

						int y = (int)Math.Round(sin);

						//Copy the base image, offseting it
						img.Composite(image, 0, Height - image.Height, CompositeOperator.Over);
						img.Composite(hans, Width - hans.Width, y, CompositeOperator.Over);

						//Add the image
						collection.Add(img);
					}

					//if (!_enableAlpha)
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
	}
}
