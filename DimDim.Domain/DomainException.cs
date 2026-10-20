namespace DimDim.Domain;
public class DomainException(string message) : Exception(message);
public class ResourceNotFoundException(string message) : Exception(message);
public class ConflictException(string message) : Exception(message);
