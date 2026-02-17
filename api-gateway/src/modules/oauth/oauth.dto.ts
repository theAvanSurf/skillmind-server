import { IsNotEmpty, IsString } from 'class-validator';
import { ApiProperty } from '@nestjs/swagger';

/**
 * Valida los query-params que llegan en el callback del proveedor OAuth.
 */
export class OAuthCallbackDto {
  @ApiProperty({ description: 'Authorization code returned by the provider' })
  @IsString()
  @IsNotEmpty()
  code: string;

  @ApiProperty({ description: 'State token for CSRF validation' })
  @IsString()
  @IsNotEmpty()
  state: string;
}
