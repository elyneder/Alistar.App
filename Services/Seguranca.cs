using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alistar.App.Services
{
    /// <summary>
    /// Centraliza operacoes de seguranca relacionadas a senha.
    /// </summary>
    /// <remarks>
    /// O BCrypt gera hashes com salt automaticamente, por isso e mais seguro
    /// do que salvar a senha em texto puro no arquivo JSON.
    /// </remarks>
    public static class Seguranca
    {
        /// <summary>
        /// Recebe a senha digitada e devolve um hash seguro para armazenamento.
        /// </summary>
        public static string CriptografarSenha(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Compara a senha digitada com o hash salvo no cadastro do usuario.
        /// </summary>
        public static bool VerificarSenha(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

    }
}
