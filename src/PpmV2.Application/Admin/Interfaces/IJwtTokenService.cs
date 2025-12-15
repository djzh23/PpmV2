using PpmV2.Application.Auth.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Admin.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(JwtUserClaims claims);
}
