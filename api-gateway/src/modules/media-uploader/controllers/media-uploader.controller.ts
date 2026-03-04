import {
  Controller,
  Get,
  Post,
  UploadedFile,
  UseGuards,
  UseInterceptors,
} from "@nestjs/common";
import { FileInterceptor } from "@nestjs/platform-express";
import { ApiBearerAuth, ApiBody, ApiConsumes, ApiOperation, ApiResponse } from "@nestjs/swagger";
import * as multer from "multer";
import { UploaderService, CloudinaryImage } from "../../../infrastucture/media/cloudinary";
import { AuthGuard } from "../../../common/guards/auth.guard";

@Controller("media-upload")
@UseGuards(AuthGuard)
@ApiBearerAuth()
export class UploaderController {
  constructor(private readonly uploaderService: UploaderService) { }

  @Post("upload")
  @ApiOperation({ summary: "Upload video file" })
  @ApiConsumes("multipart/form-data")
  @ApiBody({
    schema: {
      type: "object",
      properties: {
        file: {
          type: "string",
          format: "binary",
        },
      },
    },
  })
  @UseInterceptors(
    FileInterceptor("file", {
      // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
      storage: multer.memoryStorage(),
      limits: {
        fileSize: 100 * 1024 * 1024, // 100MB
      },
      fileFilter: (_req, file, cb) => {
        if (!file.mimetype.startsWith("video/")) {
          cb(new Error("Only video files allowed"), false);
          return;
        }
        cb(null, true);
      },
    }),
  )
  async uploadFile(@UploadedFile() file: Express.Multer.File) {
    return await this.uploaderService.uploadFileToCloud(file);
  }

  @Get('images')
  @ApiOperation({ summary: 'List all images stored in Cloudinary' })
  @ApiResponse({ status: 200, description: 'Array of image metadata' })
  async listImages(): Promise<CloudinaryImage[]> {
    return this.uploaderService.listImages();
  }
}
