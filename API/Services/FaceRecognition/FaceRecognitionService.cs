using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace API.Services.FaceRecognition
{
    public class FaceRecognitionService : IDisposable
    {
        private readonly InferenceSession _faceDetector;
        private readonly InferenceSession _faceRecognizer;
        private readonly double _cosineSimilarityThreshold = 0.36;
        private readonly float _yoloScoreThreshold = 0.5f;
        private readonly float _yoloNmsThreshold = 0.45f;
        private readonly int _yoloInputSize = 640;

        public FaceRecognitionService(string faceDetectorPath, string faceRecognizerPath)
        {
            _faceDetector = new InferenceSession(faceDetectorPath);
            _faceRecognizer = new InferenceSession(faceRecognizerPath);
        }

        public Mat[] DetectFaces(byte[] imageBytes)
        {
            using var originalMat = Mat.FromImageData(imageBytes, ImreadModes.Color);
            var resizedMat = new Mat();
            Cv2.Resize(originalMat, resizedMat, new Size(_yoloInputSize, _yoloInputSize));

            var floatData = new float[_yoloInputSize * _yoloInputSize * 3];
            resizedMat.GetArray(out floatData);

            // Chuyển đổi BGR sang RGB và chuẩn hóa
            var inputTensor = new DenseTensor<float>(new[] { 1, 3, _yoloInputSize, _yoloInputSize });
            for (int y = 0; y < _yoloInputSize; y++)
            {
                for (int x = 0; x < _yoloInputSize; x++)
                {
                    int index = (y * _yoloInputSize + x) * 3;
                    inputTensor[0, 0, y, x] = floatData[index + 2] / 255.0f; // R
                    inputTensor[0, 1, y, x] = floatData[index + 1] / 255.0f; // G
                    inputTensor[0, 2, y, x] = floatData[index + 0] / 255.0f; // B
                }
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_faceDetector.InputMetadata.Keys.First(), inputTensor)
            };

            using var results = _faceDetector.Run(inputs);
            var output = results.First().AsTensor<float>();

            var detectedFaces = new List<Mat>();
            var confidences = new List<float>();
            var boxes = new List<Rect2d>();

            // Cấu trúc output của YOLOv8: [batch_size, 6, num_detections] -> [1, 6, N]
            var outputData = output.ToArray();
            var detections = output.Dimensions[2];

            for (int i = 0; i < detections; i++)
            {
                float confidence = outputData[i * 6 + 4]; // Lấy confidence
                if (confidence >= _yoloScoreThreshold)
                {
                    float cx = outputData[i * 6 + 0];
                    float cy = outputData[i * 6 + 1];
                    float w = outputData[i * 6 + 2];
                    float h = outputData[i * 6 + 3];

                    var x = (cx - w / 2) * originalMat.Width / _yoloInputSize;
                    var y = (cy - h / 2) * originalMat.Height / _yoloInputSize;
                    w = w * originalMat.Width / _yoloInputSize;
                    h = h * originalMat.Height / _yoloInputSize;

                    boxes.Add(new Rect2d(x, y, w, h));
                    confidences.Add(confidence);
                }
            }

            // Non-Maximum Suppression (sử dụng OpenCvSharp để tiện lợi)
            int[] indices;
            if (boxes.Count > 0)
            {
                OpenCvSharp.Dnn.CvDnn.NMSBoxes(boxes, confidences, _yoloScoreThreshold, _yoloNmsThreshold, out indices);
            }
            else
            {
                indices = new int[0];
            }

            foreach (var i in indices)
            {
                var box = boxes[i];
                var rect = new Rect(
                    (int)Math.Max(0, box.X),
                    (int)Math.Max(0, box.Y),
                    (int)Math.Min(box.Width, originalMat.Width - box.X),
                    (int)Math.Min(box.Height, originalMat.Height - box.Y)
                );

                if (rect.Width > 0 && rect.Height > 0)
                {
                    detectedFaces.Add(new Mat(originalMat, rect));
                }
            }
            return detectedFaces.ToArray();
        }

        public float[] ExtractFaceFeatures(Mat faceImage)
        {
            var resizedMat = new Mat();
            Cv2.Resize(faceImage, resizedMat, new Size(112, 112));

            var floatData = new float[112 * 112 * 3];
            resizedMat.GetArray(out floatData);

            // Chuyển đổi BGR sang RGB và chuẩn hóa giá trị về khoảng [-1, 1]
            var inputTensor = new DenseTensor<float>(new[] { 1, 3, 112, 112 });
            for (int r = 0; r < 112; r++)
            {
                for (int c = 0; c < 112; c++)
                {
                    int index = (r * 112 + c) * 3;
                    inputTensor[0, 0, r, c] = (floatData[index + 2] - 127.5f) / 127.5f;
                    inputTensor[0, 1, r, c] = (floatData[index + 1] - 127.5f) / 127.5f;
                    inputTensor[0, 2, r, c] = (floatData[index + 0] - 127.5f) / 127.5f;
                }
            }

            var inputName = _faceRecognizer.InputMetadata.Keys.First();
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) };

            using var results = _faceRecognizer.Run(inputs);
            var outputTensor = results.First().AsTensor<float>();

            return outputTensor.ToArray();
        }

        public bool CompareFaces(float[] features1, float[] features2)
        {
            var similarity = CalculateCosineSimilarity(features1, features2);
            return similarity > _cosineSimilarityThreshold;
        }

        private double CalculateCosineSimilarity(float[] features1, float[] features2)
        {
            double dotProduct = 0.0;
            double norm1 = 0.0;
            double norm2 = 0.0;

            for (int i = 0; i < features1.Length; i++)
            {
                dotProduct += features1[i] * features2[i];
                norm1 += Math.Pow(features1[i], 2);
                norm2 += Math.Pow(features2[i], 2);
            }

            if (norm1 == 0 || norm2 == 0) return 0;
            return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }

        public void Dispose()
        {
            _faceDetector?.Dispose();
            _faceRecognizer?.Dispose();
        }
    }
}