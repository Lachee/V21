using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V21Bot.Magicks
{
	public interface IMagick
	{
		string Name { get; }
		string GetFilename(string username);
		byte[] Generate(string resources, MagickImage image);
	}
}
