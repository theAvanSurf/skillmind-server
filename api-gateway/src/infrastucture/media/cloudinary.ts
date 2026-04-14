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
    const isVideo = file.mimetype.startsWith('video/');

    return new Promise((resolve, reject) => {
      const uploadStream = cloudinary.uploader.upload_stream(
        {
          resource_type: "auto",
          folder: "uploads",
          // Pre-generate DASH segments for video uploads so delivery URLs are
          // ready immediately. eager_async keeps the upload response fast while
          // transcoding happens in the background.
          ...(isVideo && {
            eager: [{ streaming_profile: 'auto', format: 'mpd' }],
            eager_async: true,
          }),
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
  async listImages(): Promise<CloudinaryImage[]> {
    const result = await cloudinary.api.resources({
      resource_type: 'image',
      type: 'upload',
      max_results: 500,
    });

    return (result.resources as CloudinaryResourceItem[]).map((r) => ({
      publicId: r.public_id,
      url: r.url,
      secureUrl: r.secure_url,
      format: r.format,
      bytes: r.bytes,
      createdAt: r.created_at,
    }));
  }
}

export interface CloudinaryImage {
  publicId: string;
  url: string;
  secureUrl: string;
  format: string;
  bytes: number;
  createdAt: string;
}

interface CloudinaryResourceItem {
  public_id: string;
  url: string;
  secure_url: string;
  format: string;
  bytes: number;
  created_at: string;
}
