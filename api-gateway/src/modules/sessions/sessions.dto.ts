import { ApiProperty, ApiPropertyOptional } from "@nestjs/swagger";
import { IsNotEmpty, IsString, IsUUID } from "class-validator";
import { ProfilesDto } from "../profiles/profiles.dto";

export class DeviceDto {
    @ApiProperty({ example: 'device-abc123', description: 'Unique device identifier' })
    @IsString()
    @IsNotEmpty()
    DeviceId: string;

    @ApiProperty({ example: '123e4567-e89b-12d3-a456-426614174000', description: 'Profile ID linked to this device' })
    @IsString()
    @IsNotEmpty()
    ProfileId: string;
}

export interface SessionDto {
    SessionId: string;
    UserId: string;
    Profiles: ProfilesDto[] | null;
    ConnectedDevices: DeviceDto[] | null;
    ConnectedDevicesCount: number;
    SessionJwtToken: string;
    CreatedAt: string;
    ExpiresAt: string;
}
