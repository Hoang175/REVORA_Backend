using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using REVORA_BE.Helpers;
using System;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Implementations
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folderPath)
        {
            if (file == null || file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderPath, // Đường dẫn thư mục (ví dụ: REVORA_Media/Products/User_1)
            };

            // Thực thi upload
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Lỗi tải ảnh lên Cloudinary: {uploadResult.Error.Message}");
            }

            // Trả về link ảnh (https) sau khi up thành công
            return uploadResult.SecureUrl.ToString();
        }


        public async Task<string?> UploadVideoAsync(IFormFile file, string folderPath)
        {
            if (file == null || file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderPath,
                UseFilename = true,
                UniqueFilename = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Lỗi tải video lên Cloudinary: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }

    }
}