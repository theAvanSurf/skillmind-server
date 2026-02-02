import { Injectable } from "@nestjs/common";
import { v2 as cloudinary } from "cloudinary";
import { Readable } from "stream";
// Use Multer's File type directly, fallback to local type if import fails
type MulterFile = {
  /** Field name specified in the form */
  fieldname: string;
  /** Name of the file on the user's computer */
  originalname: string;
  /** Encoding type of the file */
  encoding: string;
  /** Mime type of the file */
  mimetype: string;
  /** Size of the file in bytes */
  size: number;
  /** A Buffer of the entire file */
  buffer: Buffer;
  /** Location of the uploaded file (if using disk storage) */
  path?: string;
};

@Injectable()
export class UploaderService {
  constructor() {
    cloudinary.config({
      cloud_name: process.env.CLOUDINARY_CLOUD_NAME,
      api_key: process.env.CLOUDINARY_API_KEY,
      api_secret: process.env.CLOUDINARY_API_SECRET,
      secure: true,
    });
  }

  uploadFileToCloud(file: MulterFile) {
    return new Promise((resolve, reject) => {
      const uploadStream = cloudinary.uploader.upload_stream(
        {
          resource_type: "auto",
          folder: "uploads",
        },
        (error, result) => {
          if (error) return reject(new Error(error?.message || 'Cloudinary upload error'));
          resolve(result);
        },
      );

      // Ensure file.buffer is a Buffer
      if (!file || typeof file.buffer === 'undefined' || !(file.buffer instanceof Buffer)) {
        return reject(new Error('Invalid file or file buffer'));
      }
      Readable.from(file.buffer).pipe(uploadStream);
    });
  }
}
