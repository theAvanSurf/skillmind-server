import { ApiProperty, ApiPropertyOptional } from "@nestjs/swagger";
import { IsBoolean, IsNumber, IsNotEmpty, IsOptional, IsString, IsUrl, Max, Min } from "class-validator";

/**
 * ProfileType mirrors SkillMind.Core.Domain.Enums.ProfileTypes
 * Adjust numeric values to match your C# enum if needed.
 */
export enum ProfileType {
    Standard = 0,
    Kids = 1,
}

export class CreateProfileDto {
    @ApiProperty({ example: 'John Doe', description: 'Display name for the profile' })
    @IsString()
    @IsNotEmpty()
    ProfileName: string;

    @ApiProperty({ example: 'https://cdn.example.com/avatar.png', description: 'URL to the profile photo' })
    @IsString()
    @IsNotEmpty()
    ProfilePhotoUrl: string;

    @ApiProperty({ example: 0, description: '0 = Standard, 1 = Kids' })
    @IsNumber()
    @Min(0)
    @Max(10)
    ProfileType: number;

    @ApiProperty({ example: false, description: 'Whether this is a kids profile' })
    @IsBoolean()
    KidsProfile: boolean;
}

export class UpdateProfileDto {
    @ApiProperty({ example: 'John Doe', description: 'Updated display name' })
    @IsString()
    @IsNotEmpty()
    ProfileName: string;

    @ApiProperty({ example: 'https://cdn.example.com/avatar.png', description: 'Updated profile photo URL' })
    @IsString()
    @IsNotEmpty()
    ProfilePhotoUrl: string;

    @ApiProperty({ example: 0, description: '0 = Standard, 1 = Kids' })
    @IsNumber()
    @Min(0)
    @Max(10)
    ProfileType: number;

    @ApiProperty({ example: false })
    @IsBoolean()
    KidsProfile: boolean;
}

export class ProfilesQueryDto {
    @ApiPropertyOptional({ example: 1, description: 'Page number' })
    @IsOptional()
    @IsNumber()
    page?: number;

    @ApiPropertyOptional({ example: 10, description: 'Items per page' })
    @IsOptional()
    @IsNumber()
    pageSize?: number;

    @ApiPropertyOptional({ example: 'John', description: 'Search term for profile name' })
    @IsOptional()
    @IsString()
    search?: string;

    @ApiPropertyOptional({ example: 'ProfileName', description: 'Field to sort by' })
    @IsOptional()
    @IsString()
    sortBy?: string;
}

export interface ProfilesDto {
    Id: string;
    UserId: string;
    ProfileName: string;
    ProfilePhotoUrl: string;
    ProfileType: number;
    KidsProfile: boolean;
}
