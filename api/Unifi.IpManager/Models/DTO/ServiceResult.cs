using System;
using System.Collections.Generic;

namespace Unifi.IpManager.Models.DTO;

public class ServiceResult<T> : ServiceResult
{
    public void MarkSuccessful(T data)
    {
        base.MarkSuccessful();
        Data = data;
    }

    public T Data { get; set; }
}


public class ServiceResult
{
    public bool Success { get; set; }

    public List<string> Errors { get; set; }

    public List<string> Messages { get; set; }

    public ServiceResult()
    {
        Errors = [];
        Messages = [];
    }

    public virtual void MarkSuccessful()
    {
        Success = true;
    }

    public virtual void MarkFailed(Exception ex)
    {
        Errors.Add(ex.Message);
        Success = false;
    }

    public virtual void MarkFailed(string error)
    {
        Errors.Add(error);
        Success = false;
    }

    public virtual void MarkFailed(IEnumerable<string> errors)
    {
        Errors.AddRange(errors);
        Success = false;
    }
}
