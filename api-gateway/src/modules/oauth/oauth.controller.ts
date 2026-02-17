import {
  Controller,
  Get,
  Param,
  Query,
  Res,
  Req,
  HttpCode,
  HttpStatus,
} from '@nestjs/common';
import { ApiTags, ApiOperation, ApiParam } from '@nestjs/swagger';
import type { Request, Response } from 'express';

import { OAuthService } from './oauth.service.js';
import { OAuthCallbackDto } from './oauth.dto.js';

@ApiTags('OAuth')
@Controller('oauth')
export class OAuthController {
  constructor(private readonly oauthService: OAuthService) {}

 

  @Get(':provider')
  @ApiOperation({ summary: 'Redirect to OAuth provider' })
  @ApiParam({ name: 'provider', example: 'google' })
  redirectToProvider(
    @Param('provider') provider: string,
    @Res() res: Response,
  ) {
    const { url, state } = this.oauthService.buildAuthorizationUrl(provider);

   
    res.cookie('oauth_state', state, {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      maxAge: 5 * 60 * 1000, 
      sameSite: 'lax',
    });

    return res.redirect(url);
  }



  @Get(':provider/callback')
  @HttpCode(HttpStatus.OK)
  @ApiOperation({ summary: 'OAuth callback — exchange code for JWT' })
  @ApiParam({ name: 'provider', example: 'google' })
  async handleCallback(
    @Param('provider') provider: string,
    @Query() query: OAuthCallbackDto,
    @Req() req: Request,
    @Res({ passthrough: true }) res: Response,
  ) {
    const storedState = req.cookies?.['oauth_state'] as string | undefined;

    const result = await this.oauthService.handleCallback(
      provider,
      query.code,
      query.state,
      storedState,
    );


    res.clearCookie('oauth_state');

    return result;
  }
}
