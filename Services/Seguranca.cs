using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Mail;
using Alistar.App.Models;

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
        private static List<ContaUsuario> contaUsuarios = ServicoAutenticacao.Contas;

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

        public static void gerarToken(string emailParaEnvioDeToken)
        {
            ContaUsuario usuarioEncontrado = contaUsuarios.FirstOrDefault(u => u.Email == emailParaEnvioDeToken);

            string token = new Random().Next(100000, 999999).ToString();

            usuarioEncontrado.Token = token;
            usuarioEncontrado.DataExpiracaoToken = DateTime.Now.AddMinutes(3);

            using var client = new SmtpClient("sandbox.smtp.mailtrap.io", 2525)
            {
                // Alterar as credenciaciais para as fornecidas pelo Mailtrap de cada usuário para evitar vazamento de credenciais
                Credentials = new NetworkCredential("c4fed5e8565447", "29622df1a99279"),
                EnableSsl = true
            };

            string title = "Token de recuperação de senha Alistar";
            string content = $"Seu token de recuperação é: {token} ! Ele irá expirar dentro de 3 minutos !\n\nCaso você não tenha solicitado esse token, desconsidere esse email !\n\nAtencionsamente,\nEquipe Alistar.app";

            try
            {
                client.Send("from@example.com", "admin@alistar.com", title, content);
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"Erro no protocolo SMTP: {ex.StatusCode}");
            }
            
        }

        public static bool ValidarCodigo(string codigo, string emailParaEnvioDeToken)
        {
            ContaUsuario usuario = contaUsuarios.FirstOrDefault(u => u.Email == emailParaEnvioDeToken);

            if (string.IsNullOrEmpty(codigo)) return false;

            
            if (codigo == usuario.Token && DateTime.Now <= usuario.DataExpiracaoToken)
            {
                usuario.Token = null;
                usuario.DataExpiracaoToken = DateTime.MinValue;
                return true;
            }

            return false;
        }
    }
}
