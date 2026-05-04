namespace Alistar.App.Models;

/// <summary>
/// Representa uma conta de entrevistador ou administrador do sistema.
/// </summary>
/// <remarks>
/// A senha armazenada nesta classe deve estar criptografada com BCrypt.
/// O login compara a senha digitada com esse hash, sem salvar senha pura.
/// </remarks>
public class ContaUsuario
{
    /// <summary>Nome exibido ou usado para identificar o entrevistador.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>E-mail usado como login.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash da senha gerado pelo BCrypt.</summary>
    public string Senha { get; set; } = string.Empty;
}
