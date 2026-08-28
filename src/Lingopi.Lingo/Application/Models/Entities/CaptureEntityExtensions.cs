namespace Lingopi.Lingo.Application.Models.Entities;

public static class CaptureEntityExtensions
{
    public static CaptureEntity SetError(this CaptureEntity entity, string errorCode, string errorMessage)
    {
        entity.Error = new CaptureErrorValue
        {
            Code = errorCode,
            Message = errorMessage
        };

        return entity;

    }

    public static CaptureEntity ClearError(this CaptureEntity entity)
    {
        entity.Error = null;
        return entity;
    }
}
