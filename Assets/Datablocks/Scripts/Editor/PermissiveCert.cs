using UnityEngine;
using System.Collections;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Override mono's built in validator to allow access to https connections
/// </summary>
public class PermissiveCert
{

    public static bool Validator(
        object sender,
	X509Certificate certificate,
	X509Chain chain,
	SslPolicyErrors policyErrors) {
	    // Just accept and move on...
	    return true;
    }

    public static void Instate() {
        ServicePointManager.ServerCertificateValidationCallback = Validator;
    }
}