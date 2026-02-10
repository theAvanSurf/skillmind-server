import { Module } from "@nestjs/common";
import { UploaderService } from "./cloudinary";
import { UploaderController } from "src/modules/media-uploader/controllers/media-uploader.controller";

@Module({
    controllers: [UploaderController],
    providers: [UploaderService],
    exports: [UploaderService]
})

export class UploaderModule {}