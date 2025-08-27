# Frontend Integration Process Flowchart

```mermaid
flowchart TD
    Start([Start: Frontend Integration]) --> CheckPrereqs{Prerequisites Met?}
    
    CheckPrereqs -->|No| InstallPrereqs[Install Node.js 20+<br/>.NET 8.0 SDK<br/>Docker Desktop]
    InstallPrereqs --> CheckPrereqs
    CheckPrereqs -->|Yes| Phase1[Phase 1: Project Structure]
    
    %% Phase 1: Project Structure Setup
    Phase1 --> CreateDir[Create src/ECommerceStore.Frontend]
    CreateDir --> MoveFiles[Move files from /frontend]
    MoveFiles --> UpdatePaths[Update relative path references]
    UpdatePaths --> UpdateSolution[Update solution file]
    UpdateSolution --> ValidateStructure{Structure Valid?}
    
    ValidateStructure -->|No| FixStructure[Fix file references<br/>and paths]
    FixStructure --> ValidateStructure
    ValidateStructure -->|Yes| Phase2[Phase 2: Aspire Integration]
    
    %% Phase 2: Aspire Integration Setup
    Phase2 --> AddNodeJSPackage[Add Aspire.Hosting.NodeJS<br/>NuGet package]
    AddNodeJSPackage --> ConfigureAppHost[Configure AddNpmApp<br/>in Program.cs]
    ConfigureAppHost --> UpdateNextConfig[Update next.config.ts<br/>for Aspire compatibility]
    UpdateNextConfig --> ConfigureEnvVars[Set up environment<br/>variable handling]
    ConfigureEnvVars --> TestAspireConfig{Aspire Config Valid?}
    
    TestAspireConfig -->|No| DebugAspireIssues[Debug configuration<br/>issues]
    DebugAspireIssues --> ConfigureAppHost
    TestAspireConfig -->|Yes| Phase3[Phase 3: Service Communication]
    
    %% Phase 3: Service Communication Setup
    Phase3 --> ConfigureAPIClient[Implement API client<br/>configuration]
    ConfigureAPIClient --> SetupServiceDiscovery[Configure dynamic<br/>API URL resolution]
    SetupServiceDiscovery --> ConfigureEnvManagement[Set up environment<br/>variable management]
    ConfigureEnvManagement --> TestServiceComm{Service Communication<br/>Working?}
    
    TestServiceComm -->|No| DebugServiceIssues[Debug service<br/>discovery issues]
    DebugServiceIssues --> SetupServiceDiscovery
    TestServiceComm -->|Yes| Phase4[Phase 4: Production Setup]
    
    %% Phase 4: Production Deployment Setup
    Phase4 --> CreateDockerfile[Create multi-stage<br/>Dockerfile]
    CreateDockerfile --> ImplementHealthCheck[Implement health<br/>check endpoint]
    ImplementHealthCheck --> ConfigureProdOptimizations[Configure production<br/>optimizations]
    ConfigureProdOptimizations --> TestDockerBuild{Docker Build<br/>Successful?}
    
    TestDockerBuild -->|No| FixDockerIssues[Fix Docker build<br/>issues]
    FixDockerIssues --> CreateDockerfile
    TestDockerBuild -->|Yes| Phase5[Phase 5: Testing & Validation]
    
    %% Phase 5: Testing and Validation
    Phase5 --> TestDevWorkflow[Test development<br/>workflow]
    TestDevWorkflow --> RunUnitTests[Run unit tests<br/>(Jest)]
    RunUnitTests --> RunE2ETests[Run E2E tests<br/>(Playwright)]
    RunE2ETests --> TestAPIIntegration[Test API integration<br/>scenarios]
    TestAPIIntegration --> ValidateAspireDashboard[Validate Aspire<br/>Dashboard integration]
    ValidateAspireDashboard --> AllTestsPassing{All Tests<br/>Passing?}
    
    AllTestsPassing -->|No| FixTestIssues[Fix failing tests<br/>and issues]
    FixTestIssues --> RunUnitTests
    AllTestsPassing -->|Yes| TestProdDeployment[Test production<br/>deployment]
    
    TestProdDeployment --> ValidateContainerHealth[Validate container<br/>health checks]
    ValidateContainerHealth --> TestServiceMesh[Test service-to-service<br/>communication]
    TestServiceMesh --> ValidatePerformance[Validate performance<br/>benchmarks]
    ValidatePerformance --> ProdValidationPassed{Production<br/>Validation Passed?}
    
    ProdValidationPassed -->|No| FixProdIssues[Fix production<br/>issues]
    FixProdIssues --> TestProdDeployment
    ProdValidationPassed -->|Yes| Phase6[Phase 6: Documentation]
    
    %% Phase 6: Documentation and Cleanup
    Phase6 --> UpdateDocs[Update README and<br/>documentation]
    UpdateDocs --> CleanupLegacy[Clean up legacy<br/>configuration]
    CleanupLegacy --> RemoveOldFrontend[Remove old frontend<br/>directory]
    RemoveOldFrontend --> OptimizePerformance[Optimize performance<br/>and bundle size]
    OptimizePerformance --> Phase7Decision{Implement Advanced<br/>Features?}
    
    %% Phase 7: Advanced Features (Optional)
    Phase7Decision -->|Yes| Phase7[Phase 7: Advanced Features]
    Phase7 --> ConfigureHMR[Configure hot module<br/>replacement]
    ConfigureHMR --> SetupDevProxy[Set up development<br/>proxy]
    SetupDevProxy --> IntegrateTelemetry[Integrate telemetry<br/>and monitoring]
    IntegrateTelemetry --> ConfigureObservability[Configure structured<br/>logging and tracing]
    ConfigureObservability --> AdvancedComplete[Advanced features<br/>complete]
    AdvancedComplete --> FinalValidation
    
    Phase7Decision -->|No| FinalValidation[Final validation<br/>and testing]
    
    %% Final Validation and Completion
    FinalValidation --> CheckSuccessCriteria{Success Criteria<br/>Met?}
    
    CheckSuccessCriteria -->|No| IdentifyGaps[Identify gaps and<br/>missing requirements]
    IdentifyGaps --> DeterminePhase{Which phase needs<br/>attention?}
    
    DeterminePhase -->|Structure| Phase1
    DeterminePhase -->|Aspire| Phase2
    DeterminePhase -->|Communication| Phase3
    DeterminePhase -->|Production| Phase4
    DeterminePhase -->|Testing| Phase5
    DeterminePhase -->|Documentation| Phase6
    DeterminePhase -->|Advanced| Phase7
    
    CheckSuccessCriteria -->|Yes| DocumentLessonsLearned[Document lessons<br/>learned]
    DocumentLessonsLearned --> CreateRollbackPlan[Create rollback<br/>plan documentation]
    CreateRollbackPlan --> IntegrationComplete([Integration Complete])
    
    %% Error Handling Paths
    FixStructure -.->|Critical Error| RollbackStructure[Rollback to<br/>original structure]
    DebugAspireIssues -.->|Critical Error| RollbackAspire[Rollback Aspire<br/>changes]
    DebugServiceIssues -.->|Critical Error| RollbackService[Rollback service<br/>configuration]
    FixDockerIssues -.->|Critical Error| RollbackDocker[Rollback Docker<br/>changes]
    FixTestIssues -.->|Critical Error| RollbackTests[Rollback to<br/>working state]
    FixProdIssues -.->|Critical Error| RollbackProd[Rollback production<br/>configuration]
    
    RollbackStructure --> EvaluateAlternative[Evaluate alternative<br/>approach]
    RollbackAspire --> EvaluateAlternative
    RollbackService --> EvaluateAlternative
    RollbackDocker --> EvaluateAlternative
    RollbackTests --> EvaluateAlternative
    RollbackProd --> EvaluateAlternative
    
    EvaluateAlternative --> AlternativeExists{Alternative<br/>Approach Available?}
    AlternativeExists -->|Yes| ImplementAlternative[Implement alternative<br/>solution]
    ImplementAlternative --> Phase1
    AlternativeExists -->|No| EscalateIssue[Escalate to senior<br/>engineer/architect]
    EscalateIssue --> IntegrationFailed([Integration Failed])
    
    %% Success Criteria Validation
    subgraph SuccessCriteria ["Success Criteria Checklist"]
        SC1[Frontend accessible via<br/>Aspire Dashboard]
        SC2[All existing functionality<br/>preserved]
        SC3[Service communication<br/>working]
        SC4[Development workflow<br/>streamlined]
        SC5[Production deployment<br/>successful]
        SC6[Performance benchmarks<br/>met]
    end
    
    %% Styling
    classDef phaseBox fill:#e1f5fe,stroke:#01579b,stroke-width:2px
    classDef decisionBox fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef errorBox fill:#ffebee,stroke:#c62828,stroke-width:2px
    classDef successBox fill:#e8f5e8,stroke:#2e7d32,stroke-width:2px
    
    class Phase1,Phase2,Phase3,Phase4,Phase5,Phase6,Phase7 phaseBox
    class CheckPrereqs,ValidateStructure,TestAspireConfig,TestServiceComm,TestDockerBuild,AllTestsPassing,ProdValidationPassed,Phase7Decision,CheckSuccessCriteria,DeterminePhase,AlternativeExists decisionBox
    class RollbackStructure,RollbackAspire,RollbackService,RollbackDocker,RollbackTests,RollbackProd,IntegrationFailed errorBox
    class IntegrationComplete successBox
```