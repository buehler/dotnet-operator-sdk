# Webhooks

Kubernetes supports various webhooks to extend the normal api behaviour
of the master api. Those are documented on the 
[kubernetes website](https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/).

`KubeOps` supports the following webhooks out of the box:
- Validator / Validation

The following documentation should give the user an overview
on how to implement a webhook what this implies to the written operator.

At the courtesy of the kubernetes website, here is a diagram of the
process that runs for admission controllers and api requests:

![admission controller phases](https://d33wubrfki0l68.cloudfront.net/af21ecd38ec67b3d81c1b762221b4ac777fcf02d/7c60e/images/blog/2019-03-21-a-guide-to-kubernetes-admission-controllers/admission-controller-phases.png)

## General

In general, if your operator contains _any_ registered (registered in the
DI) the build process that is provided via `KubeOps.targets` will
generate a CA certificate for you.

So if you add a webhook to your operator the following changes
to the normal deployment of the operator will happen:
1. During "after build" phase, the sdk will generate
   a CA-certificate for self signed certificates for you.
2. The ca certificate and the corresponding key are added
   to the deployment via kustomization config.
3. A special config is added to the deployment via
   kustomization to use https.
4. The deployment of the operator now contains an `init-container`
   that loads the `ca.pem` and `ca-key.pem` files and creates
   a server certificate. Also, a service and the corresponding
   webhook configurations are created.
5. When the operator starts, an additional https route is registered
   with the created server certificate.

When a webhook is registered, the specified operations will
trigger a POST call to the operator.

> [!NOTE]
> The certificates are generated with [cfssl](https://github.com/cloudflare/cfssl),
> an amazing tool from cloudflare that helps with the general hassle
> of creating CAs and certificates in general.

> [!NOTE]
> Make sure you commit the `ca.pem` / `ca-key.pem` file.
> During operator startup (init container) those files
> are needed. Since this represents a self signed certificate,
> and it is only used for cluster internal communication,
> it is no security issue to the system. The service is not
> exposed to the internet.

> [!NOTE]
> The `server.pem` and `server-key.pem` files are generated
> in the init container during pod startup.
> Each pod / instance of the operator gets its own server
> certificate but the CA must be shared among them.

## Local development

It is possible to test webhooks locally. For this, you need
to register the webhook via dependency injection with the corresponding
method (in the builder) and then start your operator.

The operator will run on a specific http address, depending on your
configuration.
Now, use [ngrok](https://ngrok.com/) or
[localtunnel](https://localtunnel.github.io/www/) or something
similar to create a HTTPS tunnel to your local running operator.

Now you can use the cli command of the sdk
`dotnet run -- webhooks register --base-url <<TUNNEL URL>>` to
register the webhooks under the tunnel's url.

The result is your webhook being called by the kubernetes api.
It is suggested one uses `Docker Desktop` with kubernetes.

## Validation webhook

The general idea of this webhook type is to validate an entity
before it is definitely created / updated or deleted.

Webhooks are registered in a **scoped** maner to the DI system.
They behave like asp.net api controller.

The implementation of a webhook is fairly simple:
- Create a class somewhere in your project.
- Implement the @"KubeOps.Operator.Webhooks.IValidationWebhook`1" interface.
- Define the @"KubeOps.Operator.Webhooks.IValidationWebhook`1.Operations"
  (from the interface) that the validator is interested in.
- Overwrite the corresponding methods.

> [!WARNING]
> The interface contains default implementations for _ALL_ methods.
> The default of the async methods are to call the sync ones.
> The default of the sync methods is to return a "not implemented"
> result.
> The async methods take precedence over the synchronous ones.

The return value of the validation methods are
@"KubeOps.Operator.Webhooks.ValidationResult"
objects. A result contains a boolean flag if the entity / operation
is valid or not. It may contain additional warnings (if it is valid)
that are presented to the user if the kubernetes api supports it.
If the result is invalid, one may add a custom http status code
as well as a custom error message that is presented to the user.

### Example

```c#
public class TestValidator : IValidationWebhook<EntityClass>
 {
     public ValidatedOperations Operations => ValidatedOperations.Create | ValidatedOperations.Update;

     public ValidationResult Create(EntityClass newEntity, bool dryRun) =>
         CheckSpec(newEntity)
             ? ValidationResult.Success("The username may not be foobar.")
             : ValidationResult.Fail(StatusCodes.Status400BadRequest, @"Username is ""foobar"".");

     public ValidationResult Update(EntityClass _, EntityClass newEntity, bool dryRun) =>
         CheckSpec(newEntity)
             ? ValidationResult.Success("The username may not be foobar.")
             : ValidationResult.Fail(StatusCodes.Status400BadRequest, @"Username is ""foobar"".");

     private static bool CheckSpec(EntityClass entity) => entity.Spec.Username != "foobar";
 }
```
