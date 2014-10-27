﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Drawing2D;
using System.Drawing;
//using System.Windows.Media;
using IP_Task_1;

namespace ImageOperationsPackage
{
    class ImageTransform
    {
        //MatrixTransform transMatrix;
		private Matrix transMatrix;
        private FastImage oldFastImage;
        private FastImage newFastImage;
        private Bitmap oldBitmap;
        private Bitmap newBitmap;
        private PointF[,] forwardPixelMap;
        private PointF[,] inversePixelMap;
		private float minX;
        private float minY;
        private float maxX;
		private float maxY;
        private int newWidth;
        private int newHeight;
        private int oldWidth;
        private int oldHeight;
        private float scale_x;
        private float scale_y;
        private float rotation;
        private float shear_x;
        private float shear_y;

		public ImageTransform()
		{
            scale_x = 1.0f;
            scale_y = 1.0f;
            rotation = 0.0f;
            shear_x = 0.0f;
            shear_y = 0.0f;
            refreshMatrix();
		}

        public ImageTransform(float[] transParam)
		{
			scale_x = transParam[0];
            scale_y = transParam[1];
            rotation = transParam[2];
            shear_x = transParam[3];
            shear_y = transParam[4];
            refreshMatrix();
		}

        void setShear(float shear_x, float shear_y)
        {
            this.shear_x = shear_x;
            this.shear_y = shear_y;
        }

        void setScale(float scale_x, float scale_y)
        {
            this.scale_x = scale_x;
            this.scale_y = scale_y;
        }
        void setRotation(float rotation)
        {
            this.rotation = rotation;
        }

		private void getBoundary(int height, int width)
		{
            PointF []p = new PointF[4];
            p[0] = new PointF(0, 0);
            p[1] = new PointF(width, 0);
            p[2] = new PointF(0, height);
            p[3] = new PointF(width, height);
            transMatrix.TransformPoints(p);
            minX = Math.Min(Math.Min(p[0].X, p[1].X), Math.Min(p[2].X, p[3].X));
            minY = Math.Min(Math.Min(p[0].Y, p[1].Y), Math.Min(p[2].Y, p[3].Y));
            maxX = Math.Max(Math.Max(p[0].X, p[1].X), Math.Max(p[2].X, p[3].X));
            maxY = Math.Max(Math.Max(p[0].Y, p[1].Y), Math.Max(p[2].Y, p[3].Y));
            newHeight = (int)Math.Ceiling(maxY - minY);
            newWidth = (int)Math.Ceiling(maxX - minX);
            transMatrix.Translate(-minX, -minY, MatrixOrder.Append);
		}

        private  void forwardMapping()
        {
        			// map from original to new image 
			for(int row = 0 ; row < oldHeight ; row++)
				for(int col = 0 ; col< oldWidth ; col++)
                {
                    forwardPixelMap[col, row] = new PointF(col, row);
                    transMatrix.TransformPoints(new[] { forwardPixelMap[col, row] });
                }
        }

        private void inverseMapping()
        {
            // map from new image back to original
            for (int row = 0; row < newHeight; row++)
                for (int col = 0; col < newWidth; col++)
                {
                    inversePixelMap[col, row] = new PointF(col, row);
                    transMatrix.TransformPoints(new[] { inversePixelMap[col, row] });
                }
        }

        private void interpolate()
        {
        			// interpolate
			  for (int row = 0; row < newHeight; row++)
                  for (int col = 0; col < newWidth; col++)
                  {
                      if (inversePixelMap[col, row].X >= 0 && inversePixelMap[col, row].X < oldWidth && inversePixelMap[col, row].Y >= 0 && inversePixelMap[col, row].Y < oldHeight)
                      {
                          int X1 = (int)Math.Floor(inversePixelMap[col, row].X);
                          X1 = Math.Max(0, X1);
                          int X2 = X1 + ((X1 + 1 == oldWidth) ?  0 : 1);
                          X2 = Math.Min(X2, oldWidth - 1);
                          int Y1 = (int)Math.Floor(inversePixelMap[col, row].Y);
                          Y1 = Math.Max(0, Y1);
                          int Y2 = Y1 + ((Y1 + 1 == oldHeight) ? 0 : 1);
                          Y2 = Math.Min(Y2, oldHeight - 1);

                          System.Drawing.Color P1 = oldFastImage.GetPixel(X1, Y1);
                          System.Drawing.Color P2 = oldFastImage.GetPixel(X2, Y1);
                          System.Drawing.Color P3 = oldFastImage.GetPixel(X1, Y2);
                          System.Drawing.Color P4 = oldFastImage.GetPixel(X2, Y2);

                          float Xfraction = inversePixelMap[col, row].X - (float)X1;
                          float Yfraction = inversePixelMap[col, row].Y - (float)Y1;

                          float Z1R = (float)P1.R * (1f - Xfraction) + P2.R * Xfraction;
                          float Z1G = (float)P1.G * (1f - Xfraction) + P2.G * Xfraction;
                          float Z1B = (float)P1.B * (1f - Xfraction) + P2.B * Xfraction;

                          float Z2R = (float)P3.R * (1f - Xfraction) + P4.R * Xfraction;
                          float Z2G = (float)P3.G * (1f - Xfraction) + P4.G * Xfraction;
                          float Z2B = (float)P3.B * (1f - Xfraction) + P4.B * Xfraction;


                          int R = (int)(Z1R * (1f - Yfraction) + Z2R * Yfraction);
                          int G = (int)(Z1G * (1f - Yfraction) + Z2G * Yfraction);
                          int B = (int)(Z1B * (1f - Yfraction) + Z2B * Yfraction);

                          newFastImage.SetPixel(col, row, System.Drawing.Color.FromArgb(R, G, B));
                      }
                      else
                      {
                          newFastImage.SetPixel(col, row, System.Drawing.Color.FromArgb(0, 0, 0));
                      }
                  }
        }

        public Bitmap GeometricLinearTransform(Bitmap oldBitmap)
        {
			oldWidth = oldBitmap.Width;
            oldHeight = oldBitmap.Height;
			this.oldBitmap = oldBitmap;
            getBoundary(oldHeight, oldWidth);
            oldFastImage = new FastImage(oldBitmap);

			//Build new Image
            newBitmap = new Bitmap(newWidth, newHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            newFastImage = new FastImage(newBitmap);
            forwardPixelMap = new PointF[oldWidth, oldHeight];
            inversePixelMap = new PointF[newWidth, newHeight];

            forwardMapping();
            transMatrix.Invert();
            inverseMapping();
            interpolate();

            return newFastImage.getBitmap();
        }

        public void refreshMatrix()
        {
            transMatrix = new Matrix();
            transMatrix.Scale(scale_x, scale_y, MatrixOrder.Append);
            transMatrix.Rotate(rotation, MatrixOrder.Append);
            transMatrix.Shear(shear_x, shear_y, MatrixOrder.Append);

        }
    }
}