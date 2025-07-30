# Deployment Setup

## GitHub Secrets Required

To deploy this app to Azure Container Registry, you need to set up the following GitHub secrets in your repository:

### 1. Azure Service Principal Secrets
- `AZURE_CLIENT_ID` - `27faa0cf-6a60-4609-854a-3141f6635de4`
- `AZURE_TENANT_ID` - `9a400a8f-ece5-4478-b55b-02bc608bf281`
- `AZURE_SUBSCRIPTION_ID` - `ME-MngEnvMCAP190177-robvet-1`

### 2. Azure Container Registry Secret
- `ACR_LOGIN_SERVER` - `containerregistryacr.azurecr.io`

## Quick Setup Commands

### 1. Create Azure Container Registry
```bash
# Login to Azure
az login

# Create resource group (if needed)
az group create --name myResourceGroup --location eastus

# Create container registry
az acr create --resource-group myResourceGroup --name myUniqueRegistryName --sku Basic
```

### 2. Create Service Principal for GitHub Actions
```bash
# Get your subscription ID
az account show --query id --output tsv

# Create service principal with Contributor role
az ad sp create-for-rbac --name "github-actions-sp" --role contributor --scopes /subscriptions/{your-subscription-id} --sdk-auth
```

### 3. Grant ACR Access to Service Principal
```bash
# Get the service principal object ID
az ad sp show --id {client-id-from-step-2} --query id --output tsv

# Assign AcrPush role
az role assignment create --assignee {service-principal-object-id} --role AcrPush --scope /subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.ContainerRegistry/registries/{registry-name}
```

### 4. Add GitHub Secrets
1. Go to your GitHub repository → Settings → Secrets and variables → Actions
2. Add the following secrets:
   - `AZURE_CLIENT_ID`: `27faa0cf-6a60-4609-854a-3141f6635de4`
   - `AZURE_TENANT_ID`: `9a400a8f-ece5-4478-b55b-02bc608bf281`
   - `AZURE_SUBSCRIPTION_ID`: `ME-MngEnvMCAP190177-robvet-1`
   - `ACR_LOGIN_SERVER`: `containerregistryacr.azurecr.io`

## How It Works

The workflow will:
1. Build the .NET 8 application
2. Create a Docker image using the Dockerfile in `/src`
3. Push the image to Azure Container Registry with the commit SHA as the tag
4. Run automatically on pushes to `main` or `feature-branch-fix-flow`

## Manual Testing

To test locally:
```bash
# Build and run locally
docker build -f src/Dockerfile -t purchase-order-agent .
docker run -p 8080:8080 purchase-order-agent
```
